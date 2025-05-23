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

# Windows encoding düzeltmesi
if sys.platform == "win32":
    import locale
    locale.setlocale(locale.LC_ALL, 'C')

# SSL uyarılarını kapat
warnings.filterwarnings('ignore')
os.environ['PYTHONHTTPSVERIFY'] = '0'
ssl._create_default_https_context = ssl._create_unverified_context

# Logging ayarları
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler("technical_analysis.log", encoding='utf-8'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger("technical_analysis_api")

app = Flask(__name__)
CORS(app)

# Türk hisse sembolleri
TURKISH_STOCKS = {
    'THYAO': 'THYAO.IS',
    'AKBNK': 'AKBNK.IS', 
    'ISCTR': 'ISCTR.IS',
    'GARAN': 'GARAN.IS',
    'TCELL': 'TCELL.IS',
    'TUPRS': 'TUPRS.IS',
    'ARCLK': 'ARCLK.IS',
    'FROTO': 'FROTO.IS',
    'PETKM': 'PETKM.IS',
    'KOZAL': 'KOZAL.IS'
}

def fix_symbol(symbol):
    """Sembol formatını düzelt"""
    symbol = symbol.upper()
    if symbol in TURKISH_STOCKS:
        return TURKISH_STOCKS[symbol]
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
        sma = data['Close'].rolling(window=period).mean()
        current = float(sma.iloc[-1])
        return {
            'current': current,
            'period': period,
            'values': sma.tail(30).dropna().tolist()
        } if not np.isnan(current) else None
    except:
        return None

def calculate_ema(data, period=20):
    """Üstel Hareketli Ortalama"""
    try:
        ema = data['Close'].ewm(span=period).mean()
        current = float(ema.iloc[-1])
        return {
            'current': current,
            'period': period,
            'values': ema.tail(30).dropna().tolist()
        } if not np.isnan(current) else None
    except:
        return None

def calculate_rsi(data, period=14):
    """RSI Göstergesi"""
    try:
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
        return {
            'current': current,
            'period': period,
            'values': rsi.tail(30).dropna().tolist()
        } if not np.isnan(current) else None
    except:
        return None

def calculate_macd(data):
    """MACD Göstergesi"""
    try:
        ema12 = data['Close'].ewm(span=12).mean()
        ema26 = data['Close'].ewm(span=26).mean()
        macd_line = ema12 - ema26
        signal_line = macd_line.ewm(span=9).mean()
        histogram = macd_line - signal_line
        
        return {
            'macd_line': float(macd_line.iloc[-1]),
            'signal_line': float(signal_line.iloc[-1]),
            'histogram': float(histogram.iloc[-1]),
            'macd_values': macd_line.tail(30).dropna().tolist(),
            'signal_values': signal_line.tail(30).dropna().tolist()
        }
    except:
        return None

def calculate_bollinger(data, period=20):
    """Bollinger Bantları"""
    try:
        sma = data['Close'].rolling(window=period).mean()
        std = data['Close'].rolling(window=period).std()
        upper = sma + (2 * std)
        lower = sma - (2 * std)
        
        return {
            'upper_band': float(upper.iloc[-1]),
            'middle_band': float(sma.iloc[-1]),
            'lower_band': float(lower.iloc[-1]),
            'upper_values': upper.tail(30).dropna().tolist(),
            'lower_values': lower.tail(30).dropna().tolist()
        }
    except:
        return None

def calculate_stochastic(data, period=14):
    """Stochastic Osilatör"""
    try:
        low_min = data['Low'].rolling(window=period).min()
        high_max = data['High'].rolling(window=period).max()
        
        # Division by zero kontrolü
        denominator = high_max - low_min
        denominator = denominator.replace(0, 0.0001)
        
        k_percent = 100 * (data['Close'] - low_min) / denominator
        d_percent = k_percent.rolling(window=3).mean()
        
        return {
            'k_percent': float(k_percent.iloc[-1]),
            'd_percent': float(d_percent.iloc[-1])
        }
    except:
        return None

def calculate_williams_r(data, period=14):
    """Williams %R"""
    try:
        low_min = data['Low'].rolling(window=period).min()
        high_max = data['High'].rolling(window=period).max()
        
        # Division by zero kontrolü
        denominator = high_max - low_min
        denominator = denominator.replace(0, 0.0001)
        
        willr = -100 * (high_max - data['Close']) / denominator
        
        return {
            'current': float(willr.iloc[-1])
        }
    except:
        return None

def calculate_all_indicators(data):
    """Tüm teknik göstergeleri hesapla"""
    indicators = {}
    
    # Her göstergeyi ayrı ayrı hesapla
    sma = calculate_sma(data)
    if sma: indicators['sma'] = sma
    
    ema = calculate_ema(data)
    if ema: indicators['ema'] = ema
    
    rsi = calculate_rsi(data)
    if rsi: indicators['rsi'] = rsi
    
    macd = calculate_macd(data)
    if macd: indicators['macd'] = macd
    
    bollinger = calculate_bollinger(data)
    if bollinger: indicators['bollinger'] = bollinger
    
    stochastic = calculate_stochastic(data)
    if stochastic: indicators['stochastic'] = stochastic
    
    williams = calculate_williams_r(data)
    if williams: indicators['williams_r'] = williams
    
    return indicators

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
        "name": "Teknik Analiz API",
        "version": "2.0.0",
        "status": "çalışıyor",
        "available_symbols": list(TURKISH_STOCKS.keys()),
        "endpoints": {
            "/technical-analysis/<symbol>": "Teknik analiz",
            "/price-history/<symbol>": "Fiyat geçmişi"
        }
    })

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
    logger.info("Teknik Analiz API başlatılıyor...")
    app.run(host='0.0.0.0', port=5001, debug=True) 