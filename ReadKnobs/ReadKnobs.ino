#include <stdio.h>
#include <stdlib.h>
#include <string.h>
// constants
#define alpha 0.2
//funciton defs
int calc_percentage(int pot,int* prev);
int scale(int pot);

// globlals
// Store previous value read for filtering
int prev_pot1=0;
int prev_pot2=0;
int prev_pot3=0;
int prev_pot4=0;
// current reading as a percentage
int pot1_percentage;
int pot2_percentage;
int pot3_percentage;
int pot4_percentage;
// previously sent percentage
int prev_pot1_percentage=0;
int prev_pot2_percentage=0;
int prev_pot3_percentage=0;
int prev_pot4_percentage=0;
// strings to be sent over serial
char pot1_percentage_str[sizeof(char)*4];
char pot2_percentage_str[sizeof(char)*4];
char pot3_percentage_str[sizeof(char)*4];
char pot4_percentage_str[sizeof(char)*4];
//variable for tracking time passed
unsigned long now;

void setup() {
// start serial
Serial.begin(112500);  
//declare inputs
pinMode(A4,INPUT);
pinMode(A6,INPUT);
pinMode(A5,INPUT);
pinMode(A7,INPUT);
//get current time for filter
now= millis();
}
//rescale values between 0-100 as rounding using floor prevents the percentages from reaching 100
int scale(int pot){
  pot=floor((double)pot/0.99);
  pot=min(pot,100);
  pot=max(pot,0);
  return pot;
}

// converts 10 bit integer value read by ADC to a percentage
int calc_percentage(int pot,int* prev){
   *prev=pot;
   int pot_percentage=floor(((double)pot/(double)1023)*100);
   pot_percentage=calc_log(pot_percentage);
   return pot_percentage;
}

// maps linear input to logarithmic output
int calc_log(int percentage){
  percentage=abs(100-percentage);
  double log_percentage=(log((double)percentage))/log(100)*100;
  int log_percentage_int=floor(abs(log_percentage-100));
  return log_percentage_int;
}

void loop() {
// Read potentiometer values
 int pot1=analogRead(A4);
 int pot2=analogRead(A5);
 int pot3=analogRead(A6);
 int pot4=analogRead(A7);
// filter out noise in measurements
 int pot1_debounce=(alpha*pot1)+((1-alpha)*prev_pot1);
 int pot2_debounce=(alpha*pot2)+((1-alpha)*prev_pot2);
 int pot3_debounce=(alpha*pot3)+((1-alpha)*prev_pot3);
 int pot4_debounce=(alpha*pot4)+((1-alpha)*prev_pot4);
// convert filtered measurement to a percentage of a 10 bit integer
 pot1_percentage=calc_percentage(pot1_debounce,&prev_pot1);
 pot2_percentage=calc_percentage(pot2_debounce,&prev_pot2); 
 pot3_percentage=calc_percentage(pot3_debounce,&prev_pot3);
 pot4_percentage=calc_percentage(pot4_debounce,&prev_pot4); 

 pot1_percentage=scale(pot1_percentage);
 pot2_percentage=scale(pot2_percentage);
 pot3_percentage=scale(pot3_percentage);
 pot4_percentage=scale(pot4_percentage);
// convert percentage to string
 itoa(pot1_percentage,pot1_percentage_str,10);
 itoa(pot2_percentage,pot2_percentage_str,10);
 itoa(pot3_percentage,pot3_percentage_str,10);
 itoa(pot4_percentage,pot4_percentage_str,10);

// limit the sample rate to avoid flooding the serial buffer
 if ((millis()-now)>=130){
  now=millis();
  //only send if a value changes to reduce the number of updates the desktop program has to perform
  if ((prev_pot1_percentage!=pot1_percentage)||(prev_pot2_percentage!=pot2_percentage)||(prev_pot3_percentage!=pot3_percentage)||(prev_pot4_percentage!=pot4_percentage)){
    char volumes[sizeof(char)*15];
    strcpy(volumes,pot1_percentage_str);
    strcat(volumes," ");
    strcat(volumes,pot2_percentage_str);
    strcat(volumes," ");
    strcat(volumes,pot3_percentage_str);
    strcat(volumes," ");
    strcat(volumes,pot4_percentage_str);\
    //send values over serial
    Serial.println(volumes);
  }
  //record reading sent for next itteration
  prev_pot1_percentage=pot1_percentage;
  prev_pot2_percentage=pot2_percentage;
  prev_pot3_percentage=pot3_percentage;
  prev_pot4_percentage=pot4_percentage;
}
//clear serial buffer
Serial.flush();
//if pinged with ! return ! to confirm you are connected 
  while (Serial.available()) {
    char inChar = (char)Serial.read();
    if (inChar=='!')Serial.println("!");  
}
}
