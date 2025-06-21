import logging
import pandas as pd
import numpy as np
import datetime
import yfinance as yf
from flask import Flask, jsonify, request
from flask_cors import CORS
import ssl
import os
import warnings
import sys
from sklearn.cluster import KMeans
from sklearn.preprocessing import StandardScaler, MinMaxScaler
from sklearn.ensemble import IsolationForest
from sklearn.decomposition import PCA
from sklearn.linear_model import LinearRegression
from sklearn.metrics import mean_squared_error, r2_score
from scipy import stats
from scipy.signal import find_peaks
from statsmodels.tsa.stattools import adfuller
from statsmodels.tsa.arima.model import ARIMA

# Windows encoding düzeltmesi
if sys.platform == "win32":
    import locale
    locale.setlocale(locale.LC_ALL, 'C')

# SSL uyarılarını kapat
warnings.filterwarnings('ignore')
os.environ['PYTHONHTTPSVERIFY'] = '0'
ssl._create_default_https_context = ssl._create_unverified_context

# Dosya yollarını ayarla
BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOG_PATH = os.path.join(BASE_DIR, 'logs', 'data_mining_analysis.log')

# Log klasörünü oluştur
os.makedirs(os.path.dirname(LOG_PATH), exist_ok=True)

# Logging ayarları
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler(LOG_PATH, encoding='utf-8'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger("data_mining_analysis_api")

app = Flask(__name__)
CORS(app)

def fix_symbol(symbol):
    """Sembol formatını düzelt"""
    symbol = symbol.upper()
    if not symbol.endswith('.IS'):
        symbol += '.IS'
    return symbol

def get_stock_data(symbol, days=100):
    """Hisse verilerini çek"""
    try:
        end_date = datetime.datetime.now()
        start_date = end_date - datetime.timedelta(days=days)
        
        logger.info(f"Veri çekiliyor: {symbol}")
        data = yf.download(symbol, start=start_date, end=end_date, progress=False)
        
        if data.empty:
            return None
            
        logger.info(f"Başarılı: {len(data)} veri noktası")
        return data
        
    except Exception as e:
        logger.error(f"Veri çekme hatası {symbol}: {e}")
        return None

def calculate_sma(data, period=20):
    """Basit Hareketli Ortalama"""
    try:
        if len(data) < period:
            logger.warning(f"SMA hesabı için yeterli veri yok: {len(data)} < {period}")
            return None
            
        sma = data['Close'].rolling(window=period).mean()
        current = float(sma.iloc[-1])
        
        if np.isnan(current):
            logger.warning("SMA hesabında NaN değer")
            return None
            
        return {
            'current': current,
            'period': period,
            'values': sma.tail(30).dropna().values.tolist()
        }
    except Exception as e:
        logger.error(f"SMA hesaplama hatası: {e}")
        return None

def calculate_ema(data, period=20):
    """Üstel Hareketli Ortalama"""
    try:
        if len(data) < period:
            logger.warning(f"EMA hesabı için yeterli veri yok: {len(data)} < {period}")
            return None
            
        ema = data['Close'].ewm(span=period).mean()
        current = float(ema.iloc[-1])
        
        if np.isnan(current):
            logger.warning("EMA hesabında NaN değer")
            return None
            
        return {
            'current': current,
            'period': period,
            'values': ema.tail(30).dropna().values.tolist()
        }
    except Exception as e:
        logger.error(f"EMA hesaplama hatası: {e}")
        return None

def calculate_rsi(data, period=14):
    """RSI Göstergesi"""
    try:
        if len(data) < period + 1:
            logger.warning(f"RSI hesabı için yeterli veri yok: {len(data)} < {period + 1}")
            return None
            
        delta = data['Close'].diff()
        gain = delta.where(delta > 0, 0)
        loss = -delta.where(delta < 0, 0)
        
        avg_gain = gain.rolling(window=period).mean()
        avg_loss = loss.rolling(window=period).mean()
        
        # Division by zero kontrolü
        avg_loss = avg_loss.replace(0, 0.0001)
        rs = avg_gain / avg_loss
        rsi = 100 - (100 / (1 + rs))
        
        current = float(rsi.iloc[-1])
        
        if np.isnan(current):
            logger.warning("RSI hesabında NaN değer")
            return None
            
        return {
            'current': current,
            'period': period,
            'values': rsi.tail(30).dropna().values.tolist()
        }
    except Exception as e:
        logger.error(f"RSI hesaplama hatası: {e}")
        return None

def calculate_macd(data):
    """MACD Göstergesi"""
    try:
        if len(data) < 35:  # En az 26 + 9 period
            logger.warning(f"MACD hesabı için yeterli veri yok: {len(data)} < 35")
            return None
            
        ema12 = data['Close'].ewm(span=12).mean()
        ema26 = data['Close'].ewm(span=26).mean()
        macd_line = ema12 - ema26
        signal_line = macd_line.ewm(span=9).mean()
        histogram = macd_line - signal_line
        
        macd_current = float(macd_line.iloc[-1])
        signal_current = float(signal_line.iloc[-1])
        histogram_current = float(histogram.iloc[-1])
        
        if any(np.isnan([macd_current, signal_current, histogram_current])):
            logger.warning("MACD hesabında NaN değer")
            return None
        
        return {
            'macd_line': macd_current,
            'signal_line': signal_current,
            'histogram': histogram_current,
            'macd_values': macd_line.tail(30).dropna().values.tolist(),
            'signal_values': signal_line.tail(30).dropna().values.tolist()
        }
    except Exception as e:
        logger.error(f"MACD hesaplama hatası: {e}")
        return None

def calculate_bollinger(data, period=20):
    """Bollinger Bantları"""
    try:
        if len(data) < period:
            logger.warning(f"Bollinger hesabı için yeterli veri yok: {len(data)} < {period}")
            return None
            
        sma = data['Close'].rolling(window=period).mean()
        std = data['Close'].rolling(window=period).std()
        upper = sma + (2 * std)
        lower = sma - (2 * std)
        
        upper_current = float(upper.iloc[-1])
        middle_current = float(sma.iloc[-1])
        lower_current = float(lower.iloc[-1])
        
        if any(np.isnan([upper_current, middle_current, lower_current])):
            logger.warning("Bollinger hesabında NaN değer")
            return None
        
        return {
            'upper_band': upper_current,
            'middle_band': middle_current,
            'lower_band': lower_current,
            'upper_values': upper.tail(30).dropna().values.tolist(),
            'lower_values': lower.tail(30).dropna().values.tolist()
        }
    except Exception as e:
        logger.error(f"Bollinger hesaplama hatası: {e}")
        return None

def calculate_stochastic(data, period=14):
    """Stochastic Osilatör"""
    try:
        if len(data) < period + 3:  # period + 3 for %D calculation
            logger.warning(f"Stochastic hesabı için yeterli veri yok: {len(data)} < {period + 3}")
            return None
            
        low_min = data['Low'].rolling(window=period).min()
        high_max = data['High'].rolling(window=period).max()
        
        # Division by zero kontrolü
        denominator = high_max - low_min
        denominator = denominator.replace(0, 0.0001)
        
        k_percent = 100 * (data['Close'] - low_min) / denominator
        d_percent = k_percent.rolling(window=3).mean()
        
        k_current = float(k_percent.iloc[-1])
        d_current = float(d_percent.iloc[-1])
        
        if any(np.isnan([k_current, d_current])):
            logger.warning("Stochastic hesabında NaN değer")
            return None
        
        return {
            'k_percent': k_current,
            'd_percent': d_current
        }
    except Exception as e:
        logger.error(f"Stochastic hesaplama hatası: {e}")
        return None

def calculate_williams_r(data, period=14):
    """Williams %R"""
    try:
        if len(data) < period:
            logger.warning(f"Williams %R hesabı için yeterli veri yok: {len(data)} < {period}")
            return None
            
        low_min = data['Low'].rolling(window=period).min()
        high_max = data['High'].rolling(window=period).max()
        
        # Division by zero kontrolü
        denominator = high_max - low_min
        denominator = denominator.replace(0, 0.0001)
        
        willr = -100 * (high_max - data['Close']) / denominator
        
        current = float(willr.iloc[-1])
        
        if np.isnan(current):
            logger.warning("Williams %R hesabında NaN değer")
            return None
        
        return {
            'current': current
        }
    except Exception as e:
        logger.error(f"Williams %R hesaplama hatası: {e}")
        return None

def extract_advanced_features(data):
    """Gelişmiş özellik çıkarımı (Feature Engineering)"""
    try:
        features = {}
        
        # Fiyat özellikleri
        features['price_volatility'] = float(data['Close'].std())
        features['price_mean'] = float(data['Close'].mean())
        features['price_skewness'] = float(stats.skew(data['Close']))
        features['price_kurtosis'] = float(stats.kurtosis(data['Close']))
        
        # Volume özellikleri
        features['volume_mean'] = float(data['Volume'].mean())
        features['volume_volatility'] = float(data['Volume'].std())
        # linregress için 1D array gerekli
        features['volume_trend'] = float(stats.linregress(range(len(data)), data['Volume'].values.flatten())[0])
        
        # Price-Volume ilişkisi
        price_volume_corr = data['Close'].corr(data['Volume'])
        features['price_volume_correlation'] = float(price_volume_corr) if pd.notna(price_volume_corr) else 0.0
        
        # Momentum özellikleri - güvenli indexing
        if len(data) >= 2:
            features['momentum_1d'] = float((data['Close'].iloc[-1] / data['Close'].iloc[-2] - 1) * 100)
        else:
            features['momentum_1d'] = 0.0
            
        if len(data) >= 6:
            features['momentum_5d'] = float((data['Close'].iloc[-1] / data['Close'].iloc[-6] - 1) * 100)
        else:
            features['momentum_5d'] = 0.0
            
        if len(data) >= 21:
            features['momentum_20d'] = float((data['Close'].iloc[-1] / data['Close'].iloc[-21] - 1) * 100)
        else:
            features['momentum_20d'] = 0.0
        
        # Trend özellikleri
        x = np.arange(len(data))
        # linregress için 1D array gerekli
        slope, intercept, r_value, p_value, std_err = stats.linregress(x, data['Close'].values.flatten())
        features['trend_slope'] = float(slope)
        features['trend_r_squared'] = float(r_value ** 2)
        features['trend_p_value'] = float(p_value)
        
        # High-Low spread
        features['hl_spread_mean'] = float((data['High'] - data['Low']).mean())
        features['hl_spread_volatility'] = float((data['High'] - data['Low']).std())
        
        # Price position - safe division
        price_range = data['High'].max() - data['Low'].min()
        if price_range != 0:
            features['price_position'] = float((data['Close'].iloc[-1] - data['Low'].min()) / price_range)
        else:
            features['price_position'] = 0.5
        
        # Moving average ratios
        sma_5 = data['Close'].rolling(5).mean()
        sma_20 = data['Close'].rolling(20).mean()
        if pd.notna(sma_20.iloc[-1]) and pd.notna(sma_5.iloc[-1]) and sma_20.iloc[-1] != 0:
            features['sma5_sma20_ratio'] = float(sma_5.iloc[-1] / sma_20.iloc[-1])
        else:
            features['sma5_sma20_ratio'] = 1.0
        
        logger.info(f"Çıkarılan özellik sayısı: {len(features)}")
        return features
        
    except Exception as e:
        logger.error(f"Özellik çıkarımı hatası: {e}")
        return {}

def detect_chart_patterns(data):
    """Chart pattern tanıma"""
    try:
        patterns = {}
        close_prices = data['Close'].values.flatten()
        
        # Support ve Resistance seviyeleri
        # find_peaks için 1D array gerekli
        mean_price = np.mean(close_prices)
        
        # Peaks detection - 1D array kullan
        peaks, _ = find_peaks(close_prices, distance=5)
        # Resistance için yüksek fiyatlı peakları filtrele
        if len(peaks) > 0:
            high_peaks = peaks[close_prices[peaks] >= mean_price * 0.98]
            if len(high_peaks) > 0:
                patterns['resistance_levels'] = close_prices[high_peaks].tolist()
                patterns['last_resistance'] = float(close_prices[high_peaks[-1]])
        
        # Trough detection için -close_prices kullan
        troughs, _ = find_peaks(-close_prices, distance=5)
        # Support için düşük fiyatlı troughları filtrele
        if len(troughs) > 0:
            low_troughs = troughs[close_prices[troughs] <= mean_price * 1.02]
            if len(low_troughs) > 0:
                patterns['support_levels'] = close_prices[low_troughs].tolist()
                patterns['last_support'] = float(close_prices[low_troughs[-1]])
        
        # Double top/bottom pattern detection
        if 'resistance_levels' in patterns and len(patterns['resistance_levels']) >= 2:
            last_two_peaks = np.array(patterns['resistance_levels'][-2:])
            if last_two_peaks[0] != 0 and abs(last_two_peaks[0] - last_two_peaks[1]) / last_two_peaks[0] < 0.02:  # %2 tolerance
                patterns['double_top'] = True
                patterns['double_top_level'] = float(np.mean(last_two_peaks))
        
        if 'support_levels' in patterns and len(patterns['support_levels']) >= 2:
            last_two_troughs = np.array(patterns['support_levels'][-2:])
            if last_two_troughs[0] != 0 and abs(last_two_troughs[0] - last_two_troughs[1]) / last_two_troughs[0] < 0.02:
                patterns['double_bottom'] = True
                patterns['double_bottom_level'] = float(np.mean(last_two_troughs))
        
        # Trend channel detection
        if len(data) >= 20:
            upper_channel = data['High'].rolling(10).max()
            lower_channel = data['Low'].rolling(10).min()
            # Safe division - avoid Series division
            close_values = data['Close'].values.flatten()
            upper_values = upper_channel.values.flatten()
            lower_values = lower_channel.values.flatten()
            
            # Calculate channel width safely
            channel_width_values = []
            for i in range(len(close_values)):
                if close_values[i] != 0 and not np.isnan(upper_values[i]) and not np.isnan(lower_values[i]):
                    channel_width_values.append((upper_values[i] - lower_values[i]) / close_values[i])
            
            if channel_width_values:
                patterns['channel_width'] = float(np.mean(channel_width_values))
            else:
                patterns['channel_width'] = 0.0
            
            # Boolean kontrolünü düzelt
            last_support = patterns.get('last_support', 0)
            last_resistance = patterns.get('last_resistance', float('inf'))
            current_price = float(data['Close'].iloc[-1])
            patterns['in_channel'] = bool(last_support < current_price < last_resistance)
        
        logger.info(f"Tespit edilen pattern sayısı: {len(patterns)}")
        return patterns
        
    except Exception as e:
        logger.error(f"Pattern tanıma hatası: {e}")
        return {}

def detect_anomalies(data):
    """Anomali tespiti"""
    try:
        anomalies = {}
        
        # Isolation Forest ile anomali tespiti
        features = np.column_stack([
            data['Close'].values.flatten(),
            data['Volume'].values.flatten(),
            (data['High'] - data['Low']).values.flatten()
        ])
        
        # NaN değerleri temizle
        features = features[~np.isnan(features).any(axis=1)]
        
        if len(features) > 10:
            scaler = StandardScaler()
            features_scaled = scaler.fit_transform(features)
            
            iso_forest = IsolationForest(contamination=0.1, random_state=42)
            anomaly_labels = iso_forest.fit_predict(features_scaled)
            
            anomaly_count = (anomaly_labels == -1).sum()
            anomalies['total_anomalies'] = int(anomaly_count)
            anomalies['anomaly_ratio'] = float(anomaly_count / len(features))
            
            # Son 5 günde anomali var mı?
            recent_anomalies = anomaly_labels[-5:] == -1
            anomalies['recent_anomalies'] = bool(recent_anomalies.any())
            anomalies['anomaly_score'] = float(iso_forest.score_samples(features_scaled)[-1:][0])
        
        # Statistical anomaly detection
        price_changes = data['Close'].pct_change().dropna()
        z_scores = np.abs(stats.zscore(price_changes))
        statistical_anomalies = (z_scores > 3).sum()
        anomalies['statistical_anomalies'] = int(statistical_anomalies)
        
        # Volume anomalies
        volume_z_scores = np.abs(stats.zscore(data['Volume']))
        volume_anomalies = (volume_z_scores > 3).sum()
        anomalies['volume_anomalies'] = int(volume_anomalies)
        
        logger.info(f"Tespit edilen anomali sayısı: {anomalies.get('total_anomalies', 0)}")
        return anomalies
        
    except Exception as e:
        logger.error(f"Anomali tespiti hatası: {e}")
        return {}

def perform_clustering_analysis(data, n_clusters=3):
    """Clustering analizi"""
    try:
        clustering = {}
        
        # Günlük değişimleri al
        daily_returns = data['Close'].pct_change().dropna()
        volume_changes = data['Volume'].pct_change().dropna()
        
        if len(daily_returns) < n_clusters:
            return clustering
        
        # Feature matrix oluştur - array boyutlarını eşitle
        min_length = min(len(daily_returns), len(volume_changes))
        if min_length < n_clusters:
            return clustering
            
        features = np.column_stack([
            daily_returns.values[:min_length].flatten(),
            volume_changes.values[:min_length].flatten()
        ])
        
        # NaN değerleri temizle
        features = features[~np.isnan(features).any(axis=1)]
        
        if len(features) < n_clusters:
            return clustering
        
        # K-means clustering
        scaler = StandardScaler()
        features_scaled = scaler.fit_transform(features)
        
        kmeans = KMeans(n_clusters=n_clusters, random_state=42, n_init=10)
        cluster_labels = kmeans.fit_predict(features_scaled)
        
        # Cluster istatistikleri
        clustering['cluster_centers'] = kmeans.cluster_centers_.tolist()
        clustering['cluster_labels'] = cluster_labels.tolist()
        clustering['inertia'] = float(kmeans.inertia_)
        
        # Her cluster için istatistikler
        cluster_stats = {}
        for i in range(n_clusters):
            cluster_mask = cluster_labels == i
            if cluster_mask.any():
                cluster_data = features[cluster_mask]
                cluster_stats[f'cluster_{i}'] = {
                    'size': int(cluster_mask.sum()),
                    'mean_return': float(cluster_data[:, 0].mean()),
                    'mean_volume_change': float(cluster_data[:, 1].mean()),
                    'volatility': float(cluster_data[:, 0].std())
                }
        
        clustering['cluster_statistics'] = cluster_stats
        
        # Son veri noktasının hangi cluster'a ait olduğu
        if len(features) > 0:
            last_point = features[-1:] 
            last_point_scaled = scaler.transform(last_point)
            current_cluster = int(kmeans.predict(last_point_scaled)[0])
            clustering['current_cluster'] = current_cluster
        
        logger.info(f"Clustering analizi tamamlandı. Cluster sayısı: {n_clusters}")
        return clustering
        
    except Exception as e:
        logger.error(f"Clustering analizi hatası: {e}")
        return {}

def calculate_all_indicators(data):
    """Tüm teknik göstergeleri hesapla"""
    indicators = {}
    logger.info(f"Toplam veri noktası: {len(data)}")
    
    # Her göstergeyi ayrı ayrı hesapla
    logger.info("SMA hesaplanıyor...")
    sma = calculate_sma(data)
    if sma: 
        indicators['sma'] = sma
        logger.info("SMA başarılı")
    else:
        logger.warning("SMA hesaplanamadı")
    
    logger.info("EMA hesaplanıyor...")
    ema = calculate_ema(data)
    if ema: 
        indicators['ema'] = ema
        logger.info("EMA başarılı")
    else:
        logger.warning("EMA hesaplanamadı")
    
    logger.info("RSI hesaplanıyor...")
    rsi = calculate_rsi(data)
    if rsi: 
        indicators['rsi'] = rsi
        logger.info("RSI başarılı")
    else:
        logger.warning("RSI hesaplanamadı")
    
    logger.info("MACD hesaplanıyor...")
    macd = calculate_macd(data)
    if macd: 
        indicators['macd'] = macd
        logger.info("MACD başarılı")
    else:
        logger.warning("MACD hesaplanamadı")
    
    logger.info("Bollinger hesaplanıyor...")
    bollinger = calculate_bollinger(data)
    if bollinger: 
        indicators['bollinger'] = bollinger
        logger.info("Bollinger başarılı")
    else:
        logger.warning("Bollinger hesaplanamadı")
    
    logger.info("Stochastic hesaplanıyor...")
    stochastic = calculate_stochastic(data)
    if stochastic: 
        indicators['stochastic'] = stochastic
        logger.info("Stochastic başarılı")
    else:
        logger.warning("Stochastic hesaplanamadı")
    
    logger.info("Williams %R hesaplanıyor...")
    williams = calculate_williams_r(data)
    if williams: 
        indicators['williams_r'] = williams
        logger.info("Williams %R başarılı")
    else:
        logger.warning("Williams %R hesaplanamadı")
    
    logger.info(f"Toplam hesaplanan gösterge sayısı: {len(indicators)}")
    return indicators

def statistical_tests(data):
    """İstatistiksel testler"""
    try:
        tests = {}
        
        # Augmented Dickey-Fuller test (stationarity)
        try:
            adf_result = adfuller(data['Close'].dropna())
            tests['adf_test'] = {
                'statistic': float(adf_result[0]),
                'p_value': float(adf_result[1]),
                'is_stationary': bool(adf_result[1] < 0.05),
                'critical_values': {k: float(v) for k, v in adf_result[4].items()}
            }
        except Exception as e:
            logger.warning(f"ADF test hatası: {e}")
        
        # Normality test (Shapiro-Wilk)
        daily_returns = data['Close'].pct_change().dropna()
        if len(daily_returns) > 3:
            shapiro_stat, shapiro_p = stats.shapiro(daily_returns[-50:])  # Son 50 gün
            tests['normality_test'] = {
                'statistic': float(shapiro_stat),
                'p_value': float(shapiro_p),
                'is_normal': bool(shapiro_p > 0.05)
            }
        
        # Autocorrelation test - pandas 2.x uyumlu hali
        if len(daily_returns) > 10:
            try:
                # Pandas 2.x'te autocorr() metodu kaldırıldı, manuel hesaplama
                def calculate_autocorr(series, lag):
                    if len(series) <= lag:
                        return 0.0
                    series_shifted = series.shift(lag)
                    correlation = series.corr(series_shifted)
                    return float(correlation) if pd.notna(correlation) else 0.0
                
                autocorr_1 = calculate_autocorr(daily_returns, 1)
                autocorr_5 = calculate_autocorr(daily_returns, 5)
                
                tests['autocorrelation'] = {
                    'lag_1': autocorr_1,
                    'lag_5': autocorr_5
                }
            except Exception as e:
                logger.warning(f"Autocorrelation hesaplama hatası: {e}")
                tests['autocorrelation'] = {'lag_1': 0.0, 'lag_5': 0.0}
        
        # Volume-Price relationship test
        if len(data) > 10:
            close_values = data['Close'].values.flatten()
            volume_values = data['Volume'].values.flatten()
            corr_coef, corr_p = stats.pearsonr(close_values, volume_values)
            tests['price_volume_correlation'] = {
                'correlation': float(corr_coef),
                'p_value': float(corr_p),
                'is_significant': bool(corr_p < 0.05)
            }
        
        logger.info(f"İstatistiksel testler tamamlandı. Test sayısı: {len(tests)}")
        return tests
        
    except Exception as e:
        logger.error(f"İstatistiksel testler hatası: {e}")
        return {}

def predict_price(data, days_ahead=5):
    """Fiyat tahmini"""
    try:
        predictions = {}
        
        # Linear regression ile trend tahmini
        close_prices = data['Close'].values.flatten()
        x = np.arange(len(close_prices)).reshape(-1, 1)
        y = close_prices
        
        model = LinearRegression()
        model.fit(x, y)
        
        # Gelecek günler için tahmin
        future_x = np.arange(len(close_prices), len(close_prices) + days_ahead).reshape(-1, 1)
        future_predictions = model.predict(future_x)
        
        # R² skorunu hesapla
        from sklearn.metrics import r2_score
        r2 = r2_score(y, model.predict(x))
        
        predictions['linear_trend'] = {
            'predicted_prices': future_predictions.tolist(),
            'slope': float(model.coef_[0]),
            'intercept': float(model.intercept_),
            'r2_score': float(r2),
            'confidence': 'low'  # Linear regression için düşük güven
        }
        
        # Moving average based prediction
        ma_short = data['Close'].rolling(5).mean()
        ma_long = data['Close'].rolling(20).mean()
        
        # NaN kontrolü - scalar değerler için düzeltilmiş boolean kontrolü
        ma_short_last = ma_short.iloc[-1]
        ma_long_last = ma_long.iloc[-1]
        
        if pd.notna(ma_short_last) and pd.notna(ma_long_last):
            ma_signal = 'bullish' if float(ma_short_last) > float(ma_long_last) else 'bearish'
            ma_momentum = (float(ma_short_last) / float(ma_long_last) - 1) * 100
            
            predictions['moving_average_signal'] = {
                'signal': ma_signal,
                'momentum': ma_momentum,
                'ma5': float(ma_short_last),
                'ma20': float(ma_long_last)
            }
        
        # Simple ARIMA prediction (basit versiyon)
        try:
            if len(data) > 30:
                model_arima = ARIMA(close_prices[-30:], order=(1,1,1))
                fitted_model = model_arima.fit()
                arima_forecast = fitted_model.forecast(steps=days_ahead)
                
                # ARIMA forecast result handling
                if hasattr(arima_forecast, 'values'):
                    forecast_values = arima_forecast.values.tolist()
                elif hasattr(arima_forecast, 'tolist'):
                    forecast_values = arima_forecast.tolist()
                else:
                    forecast_values = [float(arima_forecast)] if np.isscalar(arima_forecast) else list(arima_forecast)
                
                predictions['arima'] = {
                    'predicted_prices': forecast_values,
                    'confidence': 'medium'
                }
        except Exception as e:
            logger.warning(f"ARIMA prediction hatası: {e}")
        
        logger.info(f"Fiyat tahmini tamamlandı. {days_ahead} gün ilerisi")
        return predictions
        
    except Exception as e:
        logger.error(f"Fiyat tahmini hatası: {e}")
        return {}

def risk_analysis(data):
    """Risk analizi"""
    try:
        risk = {}
        
        daily_returns = data['Close'].pct_change().dropna()
        
        # Value at Risk (VaR)
        if len(daily_returns) > 10:
            var_95 = float(np.percentile(daily_returns, 5))
            var_99 = float(np.percentile(daily_returns, 1))
            
            # VaR hesaplaması için boolean indexing kullan - ambiguity düzeltildi
            var_95_mask = daily_returns <= var_95
            if var_95_mask.any():
                expected_shortfall = float(daily_returns[var_95_mask].mean())
            else:
                expected_shortfall = var_95
            
            risk['value_at_risk'] = {
                'var_95': var_95,
                'var_99': var_99,
                'expected_shortfall_95': expected_shortfall
            }
        
        # Volatility measures
        risk['volatility'] = {
            'daily': float(daily_returns.std()),
            'annualized': float(daily_returns.std() * np.sqrt(252)),
            'rolling_30d': float(daily_returns.rolling(30).std().iloc[-1]) if len(daily_returns) >= 30 else float(daily_returns.std())
        }
        
        # Sharpe ratio (risk-free rate assumed 0)
        mean_return = daily_returns.mean()
        std_return = daily_returns.std()
        # Series boolean kontrolünü düzelt
        mean_return_val = float(mean_return) if pd.notna(mean_return) else 0.0
        std_return_val = float(std_return) if pd.notna(std_return) else 0.0
        if std_return_val != 0:
            risk['sharpe_ratio'] = (mean_return_val * 252) / (std_return_val * np.sqrt(252))
        
        # Maximum drawdown
        cumulative = (1 + daily_returns).cumprod()
        running_max = cumulative.expanding().max()
        drawdown = (cumulative - running_max) / running_max
        risk['max_drawdown'] = float(drawdown.min())
        
        # Beta (market beta - using a simple market proxy)
        if len(daily_returns) > 20:
            market_returns = daily_returns  # Simplified - should use market index
            # Convert to numpy arrays to avoid pandas issues
            returns_array = daily_returns.values
            market_array = market_returns.values
            covariance_matrix = np.cov(returns_array, market_array)
            market_variance = np.var(market_array)
            # Zero division kontrolü
            if market_variance != 0:
                beta = float(covariance_matrix[0, 1] / market_variance)
                risk['beta'] = beta
        
        logger.info("Risk analizi tamamlandı")
        return risk
        
    except Exception as e:
        logger.error(f"Risk analizi hatası: {e}")
        return {}

def advanced_technical_analysis(data):
    """Gelişmiş teknik analiz"""
    try:
        advanced = {}
        
        # Fibonacci retracement levels
        high_price = data['High'].max()
        low_price = data['Low'].min()
        diff = high_price - low_price
        
        advanced['fibonacci'] = {
            'high': float(high_price),
            'low': float(low_price),
            'levels': {
                '23.6%': float(high_price - 0.236 * diff),
                '38.2%': float(high_price - 0.382 * diff),
                '50%': float(high_price - 0.5 * diff),
                '61.8%': float(high_price - 0.618 * diff),
                '78.6%': float(high_price - 0.786 * diff)
            }
        }
        
        # Pivot points
        if len(data) >= 3:
            last_high = data['High'].iloc[-2]
            last_low = data['Low'].iloc[-2]
            last_close = data['Close'].iloc[-2]
            
            pivot = (last_high + last_low + last_close) / 3
            r1 = 2 * pivot - last_low
            s1 = 2 * pivot - last_high
            r2 = pivot + (last_high - last_low)
            s2 = pivot - (last_high - last_low)
            
            advanced['pivot_points'] = {
                'pivot': float(pivot),
                'resistance_1': float(r1),
                'support_1': float(s1),
                'resistance_2': float(r2),
                'support_2': float(s2)
            }
        
        # Ichimoku cloud (simplified)
        if len(data) >= 26:
            high_9 = data['High'].rolling(9).max()
            low_9 = data['Low'].rolling(9).min()
            tenkan_sen = (high_9 + low_9) / 2
            
            high_26 = data['High'].rolling(26).max()
            low_26 = data['Low'].rolling(26).min()
            kijun_sen = (high_26 + low_26) / 2
            
            senkou_span_a = ((tenkan_sen + kijun_sen) / 2).shift(26)
            senkou_span_b = ((data['High'].rolling(52).max() + data['Low'].rolling(52).min()) / 2).shift(26)
            
            current_price = data['Close'].iloc[-1]
            
            # NaN kontrolü - scalar değerler için düzeltilmiş boolean kontrolü
            senkou_a_last = senkou_span_a.iloc[-1]
            senkou_b_last = senkou_span_b.iloc[-1]
            
            if pd.notna(senkou_a_last) and pd.notna(senkou_b_last):
                cloud_top = max(senkou_a_last, senkou_b_last)
                cloud_bottom = min(senkou_a_last, senkou_b_last)
            else:
                cloud_top = current_price
                cloud_bottom = current_price
            
            # Scalar değerlerle güvenli karşılaştırma
            # Scalar değerler için güvenli karşılaştırma
            current_price_val = float(current_price)
            cloud_top_val = float(cloud_top)
            cloud_bottom_val = float(cloud_bottom)
            
            if current_price_val > cloud_top_val:
                cloud_position = 'above'
            elif current_price_val < cloud_bottom_val:
                cloud_position = 'below'
            else:
                cloud_position = 'inside'
            
            # Scalar değerleri güvenli şekilde al
            tenkan_last = tenkan_sen.iloc[-1]
            kijun_last = kijun_sen.iloc[-1]
            
            advanced['ichimoku'] = {
                'tenkan_sen': float(tenkan_last) if pd.notna(tenkan_last) else current_price_val,
                'kijun_sen': float(kijun_last) if pd.notna(kijun_last) else current_price_val,
                'cloud_top': cloud_top_val,
                'cloud_bottom': cloud_bottom_val,
                'price_position': cloud_position
            }
        
        logger.info("Gelişmiş teknik analiz tamamlandı")
        return advanced
        
    except Exception as e:
        logger.error(f"Gelişmiş teknik analiz hatası: {e}")
        return {}

def calculate_all_indicators_with_datamining(data):
    """Tüm göstergeleri ve veri madenciliği özelliklerini hesapla"""
    try:
        result = {}
        
        # Existing technical indicators
        logger.info("Klasik teknik göstergeler hesaplanıyor...")
        classic_indicators = calculate_all_indicators(data)
        result['classic_indicators'] = classic_indicators
        
        # Data mining features
        logger.info("Gelişmiş özellikler çıkarılıyor...")
        advanced_features = extract_advanced_features(data)
        result['advanced_features'] = advanced_features
        
        logger.info("Chart pattern analizi yapılıyor...")
        chart_patterns = detect_chart_patterns(data)
        result['chart_patterns'] = chart_patterns
        
        logger.info("Anomali tespiti yapılıyor...")
        anomalies = detect_anomalies(data)
        result['anomalies'] = anomalies
        
        logger.info("Clustering analizi yapılıyor...")
        clustering = perform_clustering_analysis(data)
        result['clustering'] = clustering
        
        logger.info("İstatistiksel testler yapılıyor...")
        statistical = statistical_tests(data)
        result['statistical_tests'] = statistical
        
        logger.info("Fiyat tahmini yapılıyor...")
        predictions = predict_price(data)
        result['predictions'] = predictions
        
        logger.info("Risk analizi yapılıyor...")
        risk = risk_analysis(data)
        result['risk_analysis'] = risk
        
        logger.info("Gelişmiş teknik analiz yapılıyor...")
        advanced_technical = advanced_technical_analysis(data)
        result['advanced_technical'] = advanced_technical
        
        logger.info("Veri madenciliği analizi tamamlandı")
        return result
        
    except Exception as e:
        logger.error(f"Veri madenciliği analizi hatası: {e}")
        return {}

def calculate_signals(indicators, current_price):
    """Al/sat sinyallerini hesapla"""
    signals = {}
    
    # SMA sinyali
    if 'sma' in indicators:
        signals['sma'] = 'BUY' if current_price > indicators['sma']['current'] else 'SELL'
    
    # EMA sinyali
    if 'ema' in indicators:
        signals['ema'] = 'BUY' if current_price > indicators['ema']['current'] else 'SELL'
    
    # RSI sinyali
    if 'rsi' in indicators:
        rsi_val = indicators['rsi']['current']
        if rsi_val > 70:
            signals['rsi'] = 'SELL'
        elif rsi_val < 30:
            signals['rsi'] = 'BUY'
        else:
            signals['rsi'] = 'NEUTRAL'
    
    # MACD sinyali
    if 'macd' in indicators:
        macd_val = indicators['macd']['macd_line']
        signal_val = indicators['macd']['signal_line']
        signals['macd'] = 'BUY' if macd_val > signal_val else 'SELL'
    
    # Bollinger sinyali
    if 'bollinger' in indicators:
        upper = indicators['bollinger']['upper_band']
        lower = indicators['bollinger']['lower_band']
        if current_price > upper:
            signals['bollinger'] = 'SELL'
        elif current_price < lower:
            signals['bollinger'] = 'BUY'
        else:
            signals['bollinger'] = 'NEUTRAL'
    
    # Stochastic sinyali
    if 'stochastic' in indicators:
        k_val = indicators['stochastic']['k_percent']
        d_val = indicators['stochastic']['d_percent']
        if k_val > 80 and d_val > 80:
            signals['stochastic'] = 'SELL'
        elif k_val < 20 and d_val < 20:
            signals['stochastic'] = 'BUY'
        else:
            signals['stochastic'] = 'NEUTRAL'
    
    # Williams %R sinyali
    if 'williams_r' in indicators:
        wr_val = indicators['williams_r']['current']
        if wr_val > -20:
            signals['williams_r'] = 'SELL'
        elif wr_val < -80:
            signals['williams_r'] = 'BUY'
        else:
            signals['williams_r'] = 'NEUTRAL'
    
    # Genel sinyal
    buy_count = list(signals.values()).count('BUY')
    sell_count = list(signals.values()).count('SELL')
    neutral_count = list(signals.values()).count('NEUTRAL')
    
    if buy_count > sell_count:
        overall = 'BUY'
    elif sell_count > buy_count:
        overall = 'SELL'
    else:
        overall = 'NEUTRAL'
    
    signal_strength = max(buy_count, sell_count) / len(signals) if signals else 0
    
    return {
        'individual_signals': signals,
        'overall_signal': overall,
        'signal_strength': round(signal_strength, 2),
        'buy_signals': buy_count,
        'sell_signals': sell_count,
        'neutral_signals': neutral_count
    }

def format_price_history(data):
    """Fiyat geçmişini formatla"""
    history = []
    for date, row in data.iterrows():
        history.append({
            'date': date.strftime('%Y-%m-%d'),
            'open': float(row['Open']),
            'high': float(row['High']),
            'low': float(row['Low']),
            'close': float(row['Close']),
            'volume': int(row['Volume'])
        })
    return history

# API Endpoint'leri
@app.route('/', methods=['GET'])
def home():
    return jsonify({
        "name": "Veri Madenciliği & Teknik Analiz API",
        "version": "3.0.0",
        "status": "çalışıyor",
        "features": [
            "Klasik Teknik Analiz",
            "Gelişmiş Özellik Çıkarımı",
            "Chart Pattern Tanıma",
            "Anomali Tespiti",
            "Clustering Analizi",
            "İstatistiksel Testler",
            "Fiyat Tahmini",
            "Risk Analizi",
            "Makine Öğrenmesi"
        ],
        "endpoints": {
            "/data-mining-analysis/<symbol>": "Kapsamlı veri madenciliği analizi",
            "/technical-analysis/<symbol>": "Klasik teknik analiz",
            "/price-history/<symbol>": "Fiyat geçmişi"
        }
    })

@app.route('/data-mining-analysis/<symbol>', methods=['GET'])
def data_mining_analysis(symbol):
    try:
        # Parametreler
        period_days = int(request.args.get('period_days', 90))
        include_predictions = request.args.get('predictions', 'true').lower() == 'true'
        
        # Sembol düzelt
        symbol = fix_symbol(symbol)
        
        logger.info(f"Veri madenciliği analizi isteği: {symbol}, süre: {period_days} gün")
        
        # Veri çek
        data = get_stock_data(symbol, period_days + 50)
        if data is None or data.empty:
            return jsonify({"error": f"{symbol} için veri bulunamadı"}), 404
        
        # Yeterli veri kontrolü
        if len(data) < 30:
            return jsonify({"error": "Yeterli veri yok"}), 400
        
        # Son period_days kadar veri
        recent_data = data.tail(period_days)
        current_price = float(recent_data['Close'].iloc[-1])
        
        # Veri madenciliği analizi
        analysis_result = calculate_all_indicators_with_datamining(data)
        
        # Sinyalleri hesapla (existing function)
        signals = calculate_signals(analysis_result.get('classic_indicators', {}), current_price)
        
        # Fiyat geçmişi
        price_history = format_price_history(recent_data)
        
        result = {
            "symbol": symbol.replace('.IS', ''),
            "current_price": current_price,
            "analysis_date": datetime.datetime.now().isoformat(),
            "period_days": period_days,
            "data_points": len(recent_data),
            "analysis_type": "data_mining",
            "classic_indicators": analysis_result.get('classic_indicators', {}),
            "advanced_features": analysis_result.get('advanced_features', {}),
            "chart_patterns": analysis_result.get('chart_patterns', {}),
            "anomalies": analysis_result.get('anomalies', {}),
            "clustering": analysis_result.get('clustering', {}),
            "statistical_tests": analysis_result.get('statistical_tests', {}),
            "risk_analysis": analysis_result.get('risk_analysis', {}),
            "advanced_technical": analysis_result.get('advanced_technical', {}),
            "signals": signals,
            "price_history": price_history
        }
        
        if include_predictions:
            result["predictions"] = analysis_result.get('predictions', {})
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Veri madenciliği analizi hatası: {e}")
        return jsonify({"error": "Analiz yapılamadı"}), 500

@app.route('/technical-analysis/<symbol>', methods=['GET'])
def technical_analysis(symbol):
    try:
        # Parametreler
        period_days = int(request.args.get('period_days', 90))
        
        # Sembol düzelt
        symbol = fix_symbol(symbol)
        
        logger.info(f"Teknik analiz isteği: {symbol}, süre: {period_days} gün")
        
        # Veri çek
        data = get_stock_data(symbol, period_days + 50)
        if data is None or data.empty:
            return jsonify({"error": f"{symbol} için veri bulunamadı"}), 404
        
        # Yeterli veri kontrolü
        if len(data) < 30:
            return jsonify({"error": "Yeterli veri yok"}), 400
        
        # Son period_days kadar veri
        recent_data = data.tail(period_days)
        current_price = float(recent_data['Close'].iloc[-1])
        
        # Teknik göstergeleri hesapla
        indicators = calculate_all_indicators(data)
        
        # Sinyalleri hesapla
        signals = calculate_signals(indicators, current_price)
        
        # Fiyat geçmişi
        price_history = format_price_history(recent_data)
        
        result = {
            "symbol": symbol.replace('.IS', ''),
            "current_price": current_price,
            "analysis_date": datetime.datetime.now().isoformat(),
            "period_days": period_days,
            "data_points": len(recent_data),
            "indicators": indicators,
            "signals": signals,
            "price_history": price_history
        }
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Teknik analiz hatası: {e}")
        return jsonify({"error": "Analiz yapılamadı"}), 500

@app.route('/price-history/<symbol>', methods=['GET'])
def price_history(symbol):
    try:
        period_days = int(request.args.get('period_days', 90))
        symbol = fix_symbol(symbol)
        
        data = get_stock_data(symbol, period_days)
        if data is None or data.empty:
            return jsonify({"error": f"{symbol} için veri bulunamadı"}), 404
        
        history = format_price_history(data)
        
        return jsonify({
            "symbol": symbol.replace('.IS', ''),
            "price_history": history,
            "data_points": len(data)
        })
        
    except Exception as e:
        logger.error(f"Fiyat geçmişi hatası: {e}")
        return jsonify({"error": "Fiyat geçmişi alınamadı"}), 500

@app.errorhandler(404)
def not_found(e):
    return jsonify({"error": "Sayfa bulunamadı"}), 404

@app.errorhandler(500)  
def server_error(e):
    return jsonify({"error": "Sunucu hatası"}), 500

if __name__ == '__main__':
    logger.info("Veri Madenciliği & Teknik Analiz API başlatılıyor...")
    app.run(host='0.0.0.0', port=5001, debug=True) 