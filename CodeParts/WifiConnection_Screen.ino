#include <SPI.h>
#include <TFT_eSPI.h>
#include <XPT2046_Touchscreen.h>
#include <ctype.h>
#include <WiFi.h>
#include <ESPAsyncWebServer.h>
#include <ArduinoJson.h>
#include <Preferences.h>

Preferences preferences;
AsyncWebServer server(80);

// --- Touch külön SPI buszon ---
#define XPT2046_IRQ  36
#define XPT2046_MOSI 32
#define XPT2046_MISO 39
#define XPT2046_CLK  25
#define XPT2046_CS   33

SPIClass touchSPI(VSPI);  // vagy HSPI, ha az ütközik a kijelzővel
XPT2046_Touchscreen touch(XPT2046_CS, XPT2046_IRQ);  // irq opcionális, de gyorsítja az eseményeket

TFT_eSPI tft = TFT_eSPI();

String ssid = "";
String password = "";
bool enteringSSID = true;
bool upper = false;

int state = 0;

String setting1 = "auto";
int setting2 = 128;
String setting3 = "ESP32_Device";

// --- Képernyő billentyűzet layout ---
const char* keys[4][11] = {
  { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "." },
  { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "_" },
  { "a", "s", "d", "f", "g", "h", "j", "k", "l", "<", "^ˇ" },
  { "z", "x", "c", "v", "b", "n", "m", " ", "$", "@", ">" }
};

void setup() {
  Serial.begin(115200);

  // Touch SPI külön inicializálás
  touchSPI.begin(XPT2046_CLK, XPT2046_MISO, XPT2046_MOSI, XPT2046_CS);
  touch.begin(touchSPI);
  touch.setRotation(3);

  // TFT kijelző
  tft.init();
  tft.setRotation(1);
  tft.fillScreen(TFT_BLACK);
  tft.setTextColor(TFT_WHITE, TFT_BLACK);
  tft.setTextSize(2);

  preferences.begin("settings", false);
  String ssidlocal = preferences.getString("SSID", "");
  String passwordlocal = preferences.getString("Password", "");

  // Ha nem volt érték
  if (ssidlocal == "" || passwordlocal == "") {
    drawKeyboard();
    drawInputFields();
  } 
  else {
    ssid = ssidlocal;
    password = passwordlocal;
    state = 1;
  }

  preferences.end();
}

void drawKeyboard() {
  tft.setTextColor(TFT_WHITE, TFT_BLACK);
  tft.setTextSize(2);
  tft.fillRect(0, 100, 320, 140, TFT_DARKGREY);
  for (int row = 0; row < 4; row++) {
    for (int col = 0; col < 11; col++) {
      int keyWidth = 28;
      int x = col * keyWidth;
      int y = 100 + row * 35;
      tft.fillRect(x + 2, y + 2, keyWidth - 4, 30, TFT_LIGHTGREY);
      tft.drawRect(x + 2, y + 2, keyWidth - 4, 30, TFT_BLACK);
      tft.setCursor(x + 8, y + 10);
      if(upper){ char helper[2] = { (char)toupper(keys[row][col][0]), '\0' }; tft.print(helper); Serial.println(helper); }
      else{ tft.print(keys[row][col]); }
    }
  }
}

void drawInputFields() {
  tft.fillRect(0, 0, 320, 40, TFT_NAVY);
  tft.setTextColor(TFT_WHITE, TFT_NAVY);
  tft.setCursor(10, 10);
  tft.print("SSID: ");
  tft.print(ssid);

  tft.fillRect(0, 40, 320, 40, TFT_DARKCYAN);
  tft.setCursor(10, 50);
  tft.print("PWD : ");
  for (unsigned int i = 0; i < password.length(); i++) {
    tft.print("*");
  }
}

void stateZero(){
  if (touch.touched()) {
    TS_Point p = touch.getPoint();
    // kalibrált értékek (feltételezve, hogy rotáció 1)
    int x = map(p.x, 200, 3900, 0, 320);
    int y = map(p.y, 300, 3700, 0, 240);
    Serial.println("Vals:");
    Serial.println(x);
    Serial.println(y);
    Serial.println(p.x);
    Serial.println(p.y);
    if (y >= 100) {
      int keyWidth = 27;
      int row = (y - 100) / 35;
      int col = x / keyWidth;
      Serial.println("Calculated location:");
      Serial.println(row);
      Serial.println(col);
      if (row >= 0 && row < 4 && col >= 0 && col < 11) {
        String key = keys[row][col];
        Serial.println(key);
        if (key == "<") {
          if (enteringSSID) {
            if (ssid.length() > 0) ssid.remove(ssid.length() - 1);
          } else {
            if (password.length() > 0) password.remove(password.length() - 1);
          }
        }
        else if(key == "^ˇ" )
        {
          upper = !upper;
          Serial.println(upper);
          drawKeyboard();
        }
        else if (key == ">") {
          if (enteringSSID) {
            enteringSSID = false;
          } else {
            // Készen van az adatbevitel
            Serial.println("SSID: " + ssid);
            Serial.println("PWD : " + password);

            preferences.begin("settings", false);
            preferences.putString("SSID", ssid);
            preferences.putString("Password", password);
            preferences.end();
            
            state = 1;
          }
        } else {
          if (enteringSSID) {
            if(upper){ char helper[2] = { (char)toupper(key[0]), '\0' }; ssid += helper; }
            else{ ssid += key; }
            
          } else {
            if(upper){ char helper[2] = { (char)toupper(key[0]), '\0' };  password += helper; }
            else{ password += key; }
          }
        }
        drawInputFields();
      }
    }
    delay(200);  // alap debounce
  }
}

void stateOne(){
  tft.fillScreen(TFT_BLACK);
  tft.setTextColor(TFT_WHITE);
  tft.setCursor(10, 10);
  tft.setTextSize(2);
  tft.println("Connecting to WiFi...");
  tft.println(ssid);

  WiFi.begin(ssid, password);
  int retry = 0;

  while (WiFi.status() != WL_CONNECTED && retry < 20) {
    delay(500);
    tft.print(".");
    retry++;
  }

  if (WiFi.status() == WL_CONNECTED) {
    tft.setCursor(10, 60);
    tft.println("\nConnected!");
    tft.println(WiFi.localIP());
    setupWebServer();
  } else {
    tft.println("\nFailed to connect.");

    //Connection screen
    drawKeyboard();
    drawInputFields();
    state = 0;
  }

  state = 2;
}

void setupWebServer(){
    // GET: lekéri a konfigurációt
  server.on("/config", HTTP_GET, [](AsyncWebServerRequest *request){
    DynamicJsonDocument json(256);
    json["setting1"] = setting1;
    json["setting2"] = setting2;
    json["setting3"] = setting3;

    String response;
    serializeJson(json, response);
    request->send(200, "application/json", response);
  });

  // POST: beállítja a konfigurációt
  server.on("/config", HTTP_POST, [](AsyncWebServerRequest *request){},
    NULL,
    [](AsyncWebServerRequest *request, uint8_t *data, size_t len, size_t index, size_t total) {
      DynamicJsonDocument json(512);
      DeserializationError error = deserializeJson(json, data);
      if (error) {
        request->send(400, "application/json", "{\"error\":\"Invalid JSON\"}");
        return;
      }

      if (json.containsKey("setting1")) setting1 = json["setting1"].as<String>();
      if (json.containsKey("setting2")) setting2 = json["setting2"].as<int>();
      if (json.containsKey("setting3")) setting3 = json["setting3"].as<String>();

      request->send(200, "application/json", "{\"status\":\"Configuration updated\"}");
    }
  );

  server.begin();

  tft.println("\nWebServer Started!");
}

void stateTwo(){
  Serial.println("State 2");
  delay(2000); 
}

void loop() {
  if(state == 0){ stateZero(); }
  else if(state == 1){ stateOne(); }
  else if(state == 2) { stateTwo(); }
}
