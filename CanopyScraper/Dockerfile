FROM mcr.microsoft.com/dotnet/runtime:8.0

# Install Chromium + ChromeDriver + required libraries
RUN apt-get update && apt-get install -y \
    chromium \
    chromium-driver \
    fonts-liberation \
    libnss3 \
    libatk-bridge2.0-0 \
    libgtk-3-0 \
    libxss1 \
    libasound2 \
    libgbm1 \
    libx11-xcb1 \
    libxcb-dri3-0 \
    wget \
    unzip \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY publish/ .

# Ensure Selenium/Canopy finds Chromium and ChromeDriver
ENV CHROME_BIN=/usr/bin/chromium
ENV CHROMEDRIVER=/usr/bin/chromedriver
ENV PATH="/usr/lib/chromium/:${PATH}"

CMD ["dotnet", "CanopyScraper.dll"]
