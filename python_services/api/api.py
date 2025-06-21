import os
import logging
import numpy as np
import pandas as pd
import datetime
import json
import joblib # YENİ: Scaler'ı kaydetmek için
from flask import Flask, jsonify, request, abort
from flask_cors import CORS
import yfinance as yf
from sklearn.preprocessing import MinMaxScaler
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score

import tensorflow as tf

# TensorFlow'u sadece CPU kullanmaya zorla
tf.config.set_visible_devices([], 'GPU')
os.environ['CUDA_VISIBLE_DEVICES'] = '-1'

from tensorflow.keras.models import Sequential, load_model
from tensorflow.keras.layers import LSTM, Dense, Dropout # YENİ: Dropout eklendi
from tensorflow.keras.callbacks import EarlyStopping # YENİ: EarlyStopping eklendi
from werkzeug.middleware.proxy_fix import ProxyFix
import ssl
import urllib3

# SSL sertifika doğrulaması düzeltmesi
ssl._create_default_https_context = ssl._create_unverified_context
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# Dosya yollarını ayarla
BASE_DIR = os.path.dirname(os.path.abspath(__file__)) # DEĞİŞİKLİK: Daha basit path tanımı
MODELS_DIR = os.path.join(BASE_DIR, 'models')
LOG_PATH = os.path.join(BASE_DIR, 'logs', 'api.log')

# Klasörleri oluştur
os.makedirs(MODELS_DIR, exist_ok=True)
os.makedirs(os.path.dirname(LOG_PATH), exist_ok=True)

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler(LOG_PATH),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger("stock_prediction_api")

app = Flask(__name__)
CORS(app)
app.wsgi_app = ProxyFix(app.wsgi_app, x_for=1, x_proto=1, x_host=1)

TIME_STEP = 100  # DEĞİŞİKLİK: 100'e yuvarlandı

def convert_turkish_chars(text):
    tr_chars = {
        'İ': 'I', 'Ş': 'S', 'Ğ': 'G', 'Ü': 'U', 'Ö': 'O', 'Ç': 'C',
        'ı': 'i', 'ş': 's', 'ğ': 'g', 'ü': 'u', 'ö': 'o', 'ç': 'c'
    }
    for tr_char, en_char in tr_chars.items():
        text = text.replace(tr_char, en_char)
    return text

class StockPredictor:

    def __init__(self, stock_symbol):
        self.stock_symbol_tr = stock_symbol
        self.stock_symbol = convert_turkish_chars(stock_symbol)
        
        # DEĞİŞİKLİK: Model ve scaler yolları hisse sembolüne özel
        self.model_path = os.path.join(MODELS_DIR, f'{self.stock_symbol}_model.h5')
        self.scaler_path = os.path.join(MODELS_DIR, f'{self.stock_symbol}_scaler.gz')
        
        self.model = self._load_model()
        self.scaler = self._load_scaler()

    def _load_model(self):
        if os.path.exists(self.model_path):
            logger.info(f"Mevcut model yükleniyor: {self.model_path}")
            return load_model(self.model_path)
        logger.info(f"{self.stock_symbol} için mevcut bir model bulunamadı.")
        return None

    def _load_scaler(self): # YENİ: Scaler'ı yüklemek için fonksiyon
        if os.path.exists(self.scaler_path):
            logger.info(f"Mevcut scaler yükleniyor: {self.scaler_path}")
            return joblib.load(self.scaler_path)
        return None

    def get_stock_data(self, start_date, end_date):
        logger.info(f"{self.stock_symbol} için veri çekiliyor: {start_date} - {end_date}")
        return yf.download(self.stock_symbol, start=start_date, end=end_date)

    def prepare_data(self, data, time_step=TIME_STEP):
        X, y = [], []
        for i in range(time_step, len(data)):
            X.append(data[i - time_step:i, 0])
            y.append(data[i, 0])
        return np.array(X), np.array(y)
    
    # DEĞİŞİKLİK: Model iyileştirildi (Dropout, daha basit yapı)
    def create_model(self, input_shape):
        model = Sequential([
            LSTM(units=75, return_sequences=True, input_shape=input_shape),
            Dropout(0.1),
            LSTM(units=50, return_sequences=False),
            Dropout(0.1),
            Dense(units=25),
            Dense(units=1)
        ])
        model.compile(optimizer='adam', loss='mean_squared_error', metrics=['mae'])
        return model

    # DEĞİŞİKLİK: Eğitim mantığı tamamen yeniden yazıldı
    def train_model(self, data):
        try:
            logger.info(f"{self.stock_symbol} için model eğitiliyor...")
            
            # 1. Veriyi Ölçekle ve Scaler'ı Kaydet
            close_prices = data['Close'].values.reshape(-1, 1)
            self.scaler = MinMaxScaler(feature_range=(0, 1))
            scaled_data = self.scaler.fit_transform(close_prices)
            joblib.dump(self.scaler, self.scaler_path) # Scaler'ı diske kaydet
            logger.info(f"Scaler kaydedildi: {self.scaler_path}")

            # 2. Eğitim ve Doğrulama Setlerini Oluştur
            training_data_len = int(np.ceil(len(scaled_data) * 0.8))
            
            train_data = scaled_data[0:training_data_len]
            validation_data = scaled_data[training_data_len - TIME_STEP:]

            X_train, y_train = self.prepare_data(train_data)
            X_val, y_val = self.prepare_data(validation_data)
            
            X_train = np.reshape(X_train, (X_train.shape[0], X_train.shape[1], 1))
            X_val = np.reshape(X_val, (X_val.shape[0], X_val.shape[1], 1))

            # 3. Modeli Oluştur ve Eğit
            self.model = self.create_model((X_train.shape[1], 1))
            
            # EarlyStopping: Modelin gereksiz yere ezberlemesini önler
            early_stopping = EarlyStopping(monitor='val_loss', patience=10, restore_best_weights=True)
            
            history = self.model.fit(
                X_train, y_train,
                epochs=50, # Epoch sayısını artırabiliriz, EarlyStopping en iyi yerde durdurur
                batch_size=32,
                validation_data=(X_val, y_val),
                callbacks=[early_stopping],
                verbose=1
            )
            
            # 4. Modeli Kaydet
            self.model.save(self.model_path)
            logger.info(f"Model eğitimi tamamlandı ve kaydedildi: {self.model_path}")
            
            # 5. Performans metriklerini hesapla ve döndür
            return self.calculate_performance_metrics(X_val, y_val)

        except Exception as e:
            logger.error(f"Model eğitimi sırasında hata: {e}", exc_info=True)
            raise

    # DEĞİŞİKLİK: Metrik hesaplama mantığı basitleştirildi ve doğrulama setini kullanıyor
    def calculate_performance_metrics(self, X_val, y_val_actual_scaled):
        try:
            # Tahminleri yap
            y_val_pred_scaled = self.model.predict(X_val)
            
            # Ölçeği geri al
            y_val_actual = self.scaler.inverse_transform(y_val_actual_scaled.reshape(-1, 1))
            y_val_pred = self.scaler.inverse_transform(y_val_pred_scaled)

            # Metrikleri hesapla
            mae = mean_absolute_error(y_val_actual, y_val_pred)
            rmse = np.sqrt(mean_squared_error(y_val_actual, y_val_pred))
            r2 = r2_score(y_val_actual, y_val_pred)
            
            # "Doğruluk" metriği (gerçek değerin +/- %5 aralığındaki tahminlerin oranı)
            within_5_percent = np.mean(np.abs((y_val_pred - y_val_actual) / y_val_actual) <= 0.05) * 100

            return {
                "accuracy_within_5_percent": float(within_5_percent),
                "mae": float(mae),
                "rmse": float(rmse),
                "r2_score": float(r2)
            }
        except Exception as e:
            logger.warning(f"Performans metrikleri hesaplanırken hata: {e}")
            return {"accuracy_within_5_percent": 0.0, "mae": 0.0, "rmse": 0.0, "r2_score": 0.0}

    # DEĞİŞİKLİK: Ana tahmin fonksiyonu tamamen yeniden yapılandırıldı
    def predict_next_day(self, force_retrain=False):
        try:
            # 1. Geniş bir tarih aralığında veri çek
            end_date = datetime.datetime.now()
            start_date = end_date - datetime.timedelta(days=7 * 365) # Son 10 yıllık veri
            data = self.get_stock_data(start_date.strftime("%Y-%m-%d"), end_date.strftime("%Y-%m-%d"))

            if data.empty or len(data) < TIME_STEP * 2: # Eğitim için minimum veri kontrolü
                raise ValueError(f"{self.stock_symbol} için yeterli geçmiş veri bulunamadı (en az {TIME_STEP*2} gün gerekli).")

            # 2. Modelin eğitilmesi gerekip gerekmediğini kontrol et
            performance_metrics = {}
            if self.model is None or self.scaler is None or force_retrain:
                performance_metrics = self.train_model(data)
            else:
                # Eğer model varsa, mevcut test verisiyle metrikleri tekrar hesapla
                logger.info("Mevcut model kullanılıyor, performans metrikleri hesaplanıyor...")
                training_data_len = int(np.ceil(len(data) * 0.8))
                scaled_data = self.scaler.transform(data['Close'].values.reshape(-1, 1))
                validation_data = scaled_data[training_data_len - TIME_STEP:]
                X_val, y_val = self.prepare_data(validation_data)
                X_val = np.reshape(X_val, (X_val.shape[0], X_val.shape[1], 1))
                performance_metrics = self.calculate_performance_metrics(X_val, y_val)


            # 3. Sonraki gün için tahminde bulun
            close_prices = data['Close'].values.reshape(-1, 1)
            scaled_data = self.scaler.transform(close_prices) # Tüm veriyi aynı scaler ile dönüştür
            
            last_sequence = scaled_data[-TIME_STEP:]
            last_sequence = np.reshape(last_sequence, (1, TIME_STEP, 1))
            
            predicted_scaled_price = self.model.predict(last_sequence)
            predicted_price = self.scaler.inverse_transform(predicted_scaled_price)[0][0]
            
            # 4. Sonuçları hazırla
            last_actual_price = float(data['Close'].iloc[-1])  # FutureWarning düzeltmesi
            price_change = float(predicted_price - last_actual_price)  # FutureWarning düzeltmesi
            percent_change = float((price_change / last_actual_price) * 100)  # FutureWarning düzeltmesi

            result = {
                "symbol": self.stock_symbol_tr,
                "predicted_price": float(predicted_price),
                "current_price": last_actual_price,
                "price_change": price_change,
                "percent_change": percent_change,
                "prediction_date": (data.index[-1] + datetime.timedelta(days=1)).strftime("%Y-%m-%d"),
                "last_close_date": data.index[-1].strftime("%Y-%m-%d"),
                "data_points": len(data),
                # Performance metrics'i düz yapıya çevir - C# mapping için
                "accuracy": float(performance_metrics.get("accuracy_within_5_percent", 0)),
                "mae": float(performance_metrics.get("mae", 0)),
                "rmse": float(performance_metrics.get("rmse", 0)),
                "r2": float(performance_metrics.get("r2_score", 0))
            }
            return result

        except Exception as e:
            logger.error(f"Tahmin hatası ({self.stock_symbol}): {e}", exc_info=True)
            raise


# DEĞİŞİKLİK: API endpoint'leri daha mantıklı hale getirildi
@app.route('/', methods=['GET'])
def home():
    return jsonify({
        "name": "Stock Price Prediction API",
        "version": "1.1.0",
        "endpoints": {
            "/predict": "GET - Predict next day's stock price (params: symbol, force_retrain (optional, true/false))",
            "/": "GET - This help message"
        }
    })

@app.route('/predict', methods=['GET'])
def predict_endpoint():
    stock_symbol = request.args.get('symbol')
    force_retrain_str = request.args.get('force_retrain', 'false').lower()
    
    if not stock_symbol:
        return jsonify({"error": "Hisse sembolü ('symbol') parametresi zorunludur."}), 400
        
    if force_retrain_str not in ['true', 'false']:
        return jsonify({"error": "'force_retrain' parametresi 'true' ya da 'false' olmalıdır."}), 400
        
    force_retrain = force_retrain_str == 'true'

    try:
        logger.info(f"Tahmin isteği: {stock_symbol}, Yeniden Eğitim Zorunlu: {force_retrain}")
        
        # Her istek için yeni bir StockPredictor nesnesi oluşturulur
        predictor = StockPredictor(stock_symbol)
        result = predictor.predict_next_day(force_retrain)
        
        # Sonucu daha okunaklı yazdırmak için
        print(json.dumps(result, indent=4))
        
        return jsonify(result)
        
    except ValueError as e:
        logger.warning(f"Değer hatası: {e}")
        return jsonify({"error": str(e)}), 400
    except Exception as e:
        logger.error(f"Beklenmedik bir hata oluştu: {e}", exc_info=True)
        return jsonify({"error": "Sunucuda beklenmedik bir hata oluştu."}), 500

@app.errorhandler(404)
def not_found(e):
    return jsonify({"error": "Endpoint bulunamadı"}), 404

if __name__ == '__main__':
    logger.info("Hisse Tahmin API sunucusu başlatılıyor")
    app.run(host='0.0.0.0', port=5000, debug=False)