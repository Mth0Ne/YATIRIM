import os
import logging
import numpy as np
import pandas as pd
import datetime
import json
from flask import Flask, jsonify, request, abort
from flask_cors import CORS
import yfinance as yf
from sklearn.preprocessing import MinMaxScaler
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score
from tensorflow.keras.models import Sequential, load_model
from tensorflow.keras.layers import LSTM, Dense
from werkzeug.middleware.proxy_fix import ProxyFix
import ssl
import urllib3

# SSL certificate verification fix for Windows
ssl._create_default_https_context = ssl._create_unverified_context
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler("api.log"),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger("stock_prediction_api")

app = Flask(__name__)
CORS(app)
app.wsgi_app = ProxyFix(app.wsgi_app, x_for=1, x_proto=1, x_host=1)

MODEL_PATH = 'lstm_model.h5'
TIME_STEP = 90  # Daha uzun pattern yakalamak için artırıldı

def convert_turkish_chars(text):
    tr_chars = {
        'İ': 'I', 'Ş': 'S', 'Ğ': 'G', 'Ü': 'U', 'Ö': 'O', 'Ç': 'C', 
        'ı': 'i', 'ş': 's', 'ğ': 'g', 'ü': 'u', 'ö': 'o', 'ç': 'c'
    }
    for tr_char, en_char in tr_chars.items():
        text = text.replace(tr_char, en_char)
    return text

class StockPredictor:
    
    def __init__(self):
        self.model = self._load_model()
        
    def _load_model(self):
        if os.path.exists(MODEL_PATH):
            logger.info(f"Loading existing model from {MODEL_PATH}")
            return load_model(MODEL_PATH)
        logger.info("No existing model found")
        return None
    
    def get_stock_data(self, stock_symbol, start_date, end_date):
        stock_symbol = convert_turkish_chars(stock_symbol)
        logger.info(f"Fetching data for {stock_symbol} from {start_date} to {end_date}")
        return yf.download(stock_symbol, start=start_date, end=end_date)
    
    def preprocess_data(self, data):
        if data.empty:
            raise ValueError("No data available for this stock")
            
        scaler = MinMaxScaler(feature_range=(0, 1))
        scaled_data = scaler.fit_transform(data[['Close']].values)
        return scaled_data, scaler
    
    def prepare_data(self, scaled_data, time_step=TIME_STEP):
        if len(scaled_data) <= time_step:
            raise ValueError(f"Insufficient data: {len(scaled_data)} points available, {time_step} required")
            
        X, y = [], []
        for i in range(time_step, len(scaled_data)):
            X.append(scaled_data[i - time_step:i, 0])
            y.append(scaled_data[i, 0])
            
        X = np.array(X)
        y = np.array(y)
        X = np.reshape(X, (X.shape[0], X.shape[1], 1))
        return X, y
    
    def create_model(self, input_shape):
        model = Sequential()
        model.add(LSTM(units=100, return_sequences=True, input_shape=input_shape))
        model.add(LSTM(units=50, return_sequences=True))
        model.add(LSTM(units=25, return_sequences=False))
        model.add(Dense(units=1))
        model.compile(optimizer='adam', loss='mean_squared_error', metrics=['mae'])
        return model
    
    def train_model(self, stock_symbol, start_date, end_date):
        try:
            logger.info(f"Training model for {stock_symbol}")
            
            train_start = (datetime.datetime.strptime(start_date, "%Y-%m-%d") - 
                          datetime.timedelta(days=730)).strftime("%Y-%m-%d")
            
            data = self.get_stock_data(stock_symbol, train_start, end_date)
            scaled_data, scaler = self.preprocess_data(data)
            
            X, y = self.prepare_data(scaled_data)
            
            model = self.create_model((X.shape[1], 1))
            
            model.fit(X, y, epochs=100, batch_size=16, validation_split=0.2, verbose=1)
            
            model.save(MODEL_PATH)
            self.model = model
            
            logger.info(f"Model training completed for {stock_symbol}")
            return model, scaler
            
        except Exception as e:
            logger.error(f"Error training model: {str(e)}")
            raise
    
    def predict(self, stock_symbol, start_date, end_date):
        try:
            data = self.get_stock_data(stock_symbol, start_date, end_date)
            
            if data.empty:
                raise ValueError(f"No data available for {stock_symbol}")
                
            scaled_data, scaler = self.preprocess_data(data)
            
            if self.model is None or len(scaled_data) < TIME_STEP:
                logger.info(f"Training new model for {stock_symbol}")
                self.model, scaler = self.train_model(stock_symbol, start_date, end_date)
            
            X, y = self.prepare_data(scaled_data)
            
            if len(X) == 0:
                raise ValueError("Not enough data points for prediction")
            
            # Predict for the last point (future prediction)
            predicted_scaled = self.model.predict(X[-1].reshape(1, X.shape[1], 1))
            predicted_price = scaler.inverse_transform(predicted_scaled)
            predicted_price = float(predicted_price[0][0])
            
            # Calculate performance metrics on validation data
            performance_metrics = self.calculate_performance_metrics(X, y, scaler)
            
            last_actual_price = data['Close'].iloc[-1]
            price_change = predicted_price - last_actual_price
            percent_change = (price_change / last_actual_price) * 100
            
            result = {
                "symbol": stock_symbol,
                "predicted_price": predicted_price,
                "current_price": float(last_actual_price),
                "price_change": float(price_change),
                "percent_change": float(percent_change),
                "prediction_date": datetime.datetime.now().strftime("%Y-%m-%d"),
                "last_close_date": data.index[-1].strftime("%Y-%m-%d"),
                "data_points": len(data),
                # Performance metrics
                "accuracy": performance_metrics["accuracy"],
                "mae": performance_metrics["mae"],
                "rmse": performance_metrics["rmse"],
                "r2": performance_metrics["r2"]
            }
            
            return result
            
        except Exception as e:
            logger.error(f"Prediction error for {stock_symbol}: {str(e)}")
            raise
    
    def calculate_performance_metrics(self, X, y_actual, scaler):
        """Calculate model performance metrics"""
        try:
            # Use last 20% of data for validation metrics
            val_size = max(1, len(X) // 5)
            X_val = X[-val_size:]
            y_val = y_actual[-val_size:]
            
            # Predict on validation data
            y_pred_scaled = self.model.predict(X_val)
            
            # Convert back to original scale
            y_actual_original = scaler.inverse_transform(y_val.reshape(-1, 1)).flatten()
            y_pred_original = scaler.inverse_transform(y_pred_scaled).flatten()
            
            # Calculate metrics
            mae = float(mean_absolute_error(y_actual_original, y_pred_original))
            mse = float(mean_squared_error(y_actual_original, y_pred_original))
            rmse = float(np.sqrt(mse))
            r2 = float(r2_score(y_actual_original, y_pred_original))
            
            # Calculate accuracy as percentage of predictions within 5% of actual values
            percentage_errors = np.abs((y_actual_original - y_pred_original) / y_actual_original) * 100
            accuracy = np.mean(percentage_errors <= 5.0)  # % of predictions within 5% error
            
            # Handle NaN/inf values to ensure valid JSON
            def safe_float(value):
                if np.isnan(value) or np.isinf(value):
                    return 0.0
                return float(value)
            
            return {
                "accuracy": safe_float(accuracy),
                "mae": safe_float(mae),
                "rmse": safe_float(rmse),
                "r2": safe_float(r2)
            }
            
        except Exception as e:
            logger.warning(f"Error calculating performance metrics: {str(e)}")
            # Return default metrics if calculation fails
            return {
                "accuracy": 0.0,
                "mae": 0.0,
                "rmse": 0.0,
                "r2": 0.0
            }

predictor = StockPredictor()

@app.route('/', methods=['GET'])
def home():
    return jsonify({
        "name": "Stock Price Prediction API",
        "version": "1.0.0",
        "endpoints": {
            "/predict": "GET - Predict stock price (params: symbol, start, end)",
            "/": "GET - This help message"
        }
    })

@app.route('/predict', methods=['GET'])
def predict_endpoint():
    try:
        stock_symbol = request.args.get('symbol', 'ISCTR.BIST')
        stock_symbol = convert_turkish_chars(stock_symbol)
        start_date = request.args.get('start', '2020-01-01')
        end_date = request.args.get('end', '2023-01-01')
        
        logger.info(f"Prediction request for {stock_symbol} from {start_date} to {end_date}")
        
        try:
            datetime.datetime.strptime(start_date, "%Y-%m-%d")
            datetime.datetime.strptime(end_date, "%Y-%m-%d")
        except ValueError:
            return jsonify({"error": "Invalid date format. Use YYYY-MM-DD"}), 400
            
        result = predictor.predict(stock_symbol, start_date, end_date)
        
        print(f"\n--- PREDICTION RESULTS ---")
        print(f"Symbol: {result['symbol']}")
        print(f"Current Price: {result['current_price']:.2f}")
        print(f"Predicted Price: {result['predicted_price']:.2f}")
        print(f"Change: {result['price_change']:.2f} ({result['percent_change']:.2f}%)")
        print(f"Prediction Date: {result['prediction_date']}")
        print(f"Last Close Date: {result['last_close_date']}")
        print(f"Data Points Used: {result['data_points']}")
        print("-------------------------\n")
        
        return jsonify(result)
        
    except ValueError as e:
        logger.warning(f"Value error: {str(e)}")
        return jsonify({"error": str(e)}), 400
    except Exception as e:
        logger.error(f"Unexpected error: {str(e)}")
        return jsonify({"error": "An unexpected error occurred"}), 500

@app.errorhandler(404)
def not_found(e):
    return jsonify({"error": "Endpoint not found"}), 404

@app.errorhandler(500)
def server_error(e):
    return jsonify({"error": "Internal server error"}), 500

if __name__ == '__main__':
    logger.info("Starting Stock Prediction API server")
    app.run(host='0.0.0.0', port=5000, debug=False)