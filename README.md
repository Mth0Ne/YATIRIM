# SmartBIST - Borsa İstanbul Stock Prediction Application

A full-stack application for tracking and predicting Borsa Istanbul stocks with an ASP.NET Core backend and a Python-based prediction API.

## Overview

SmartBIST combines:
- An ASP.NET Core web application for portfolio management and stock tracking
- A Python-based machine learning API for stock price predictions
- Integration between both systems for a seamless user experience

## Prerequisites

### For the ASP.NET Core Application
- .NET 6 SDK or later
- SQL Server (Local or Express)
- Visual Studio 2022 or JetBrains Rider

### For the Python Prediction API
- Python 3.8 or later
- Required packages: Flask, yfinance, numpy, scikit-learn, tensorflow, pandas

## Setup and Running the Application

### 1. Setting up the Python Prediction API

Install the required Python packages:

```bash
pip install flask yfinance numpy scikit-learn tensorflow pandas
```

Start the Python prediction API:

```bash
python api.py
```

The API will run on http://localhost:5000 by default. You can test it with:
http://localhost:5000/predict?symbol=ISCTR.BIST&start=2023-01-01&end=2023-12-31

### 2. Setting up the ASP.NET Core Application

1. Clone the repository
2. Open the solution in Visual Studio or Rider
3. Update the connection string in `appsettings.json` if needed
4. Run database migrations:
   ```
   dotnet ef database update
   ```
5. Build and run the application

The application will connect to the Python API automatically for predictions.

## Using the Application

1. Register and login to the application
2. Browse available stocks
3. Create a portfolio and add stocks to it
4. Navigate to the Predictions section to create new stock predictions
5. View your prediction results with charts and metrics

## Configuration

You can modify the Python API URL in the `appsettings.json` file:

```json
"ApiSettings": {
  "StockPredictionApiUrl": "http://localhost:5000"
}
```

## Troubleshooting

- If you encounter connection errors to the Python API, make sure it's running at the configured URL
- For database connection errors, check your SQL Server connection string
- If prediction data isn't displaying correctly, check the API logs for potential data format issues

## Architecture

The application follows a clean architecture approach:
- Core: Domain models and interfaces
- Application: Business logic and services
- Infrastructure: Data access, external services
- WebUI: User interface and controllers

The Python API uses a Long Short-Term Memory (LSTM) neural network model for stock price predictions. 