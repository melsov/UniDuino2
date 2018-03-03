#define DELAY 50


//Accelerometer calibration:
//From tests:
//512 seems to correspond more or less to 0 acceleration.
//spread between min and max readings around 600
// #define OFFSET (512.0)
// #define RANGE (600.0) 


float readings[3];
int xyzPins[] = {A3, A2, A1 };

struct MinMax{
  int min = 999999, max = -999999;

  void add(int i){
    if(i< min) {
      min = i;
    }
    if(i > max){
      max = i;
    }
  }

  void print() {
    Serial.print("min: "); Serial.print(min); Serial.print("max: "); Serial.print(max); 
    Serial.print("Range: "); Serial.print(range()); Serial.print("avg: "); Serial.print(average()); 
    Serial.println();
  }

  int range() { return max - min; }

  int average() {return min + range() / 2; }

};

MinMax ranges[3];

void setup(void)
{
  Serial.begin(115200);
  Serial.println("SparkFunAccelerometer"); Serial.println("");

  delay(1000);
}

void loop(void)
{

  for(int i=0;i<3;++i){
    readings[i]=getAxis(i);
  }

  Serial.print(F("Orientation: "));
  for(int i=0; i<3;++i){
    Serial.print((float)readings[i]);
    Serial.print(F(" "));
  }
  Serial.println(F(""));

  delay(DELAY);
}

float getAxis(int axis) {
  return analogRead(xyzPins[axis]); // - OFFSET)/(float)RANGE;
}