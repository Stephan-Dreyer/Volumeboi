 #include <stdio.h>
#include <stdlib.h>
#include <string.h>
// constants
#define thresh 10
#define alpha 0.8
//funciton defs
int calc_percentage(int pot,int prev);
int scale(int pot);

// globlas
int prev_pot1=0;
int prev_pot2=0;
int prev_pot3=0;
int prev_pot4=0;
int pot1_percentage;
int pot2_percentage;
int pot3_percentage;
int pot4_percentage;
int prev_pot1_percentage=0;
int prev_pot2_percentage=0;
int prev_pot3_percentage=0;
int prev_pot4_percentage=0;
char pot1_percentage_str[sizeof(char)*4];
char pot2_percentage_str[sizeof(char)*4];
char pot3_percentage_str[sizeof(char)*4];
char pot4_percentage_str[sizeof(char)*4];
unsigned long now;
void setup() {
  Serial.begin(112500);
pinMode(A4,INPUT);
pinMode(A6,INPUT);
pinMode(A5,INPUT);
pinMode(A7,INPUT);
now= millis();
}

void loop() {
 int pot1=analogRead(A4);
 int pot2=analogRead(A5);
 int pot3=analogRead(A6);
 int pot4=analogRead(A7);

 pot1_percentage=calc_percentage(pot1,prev_pot1);
 pot2_percentage=calc_percentage(pot2,prev_pot2); 
  pot3_percentage=calc_percentage(pot3,prev_pot3);
 pot4_percentage=calc_percentage(pot4,prev_pot4); 

 int pot1_percentage_debounce=(alpha*pot1_percentage)+((1-alpha)*prev_pot1_percentage);
 int pot2_percentage_debounce=(alpha*pot2_percentage)+((1-alpha)*prev_pot2_percentage);
  int pot3_percentage_debounce=(alpha*pot3_percentage)+((1-alpha)*prev_pot3_percentage);
 int pot4_percentage_debounce=(alpha*pot4_percentage)+((1-alpha)*prev_pot4_percentage);

 pot1_percentage_debounce=scale(pot1_percentage_debounce);
 pot2_percentage_debounce=scale(pot2_percentage_debounce);
  pot3_percentage_debounce=scale(pot3_percentage_debounce);
 pot4_percentage_debounce=scale(pot4_percentage_debounce);



 
itoa(pot1_percentage_debounce,pot1_percentage_str,10);
itoa(pot2_percentage_debounce,pot2_percentage_str,10);
itoa(pot3_percentage_debounce,pot3_percentage_str,10);
itoa(pot4_percentage_debounce,pot4_percentage_str,10);




if ((millis()-now)>=180){
  
 now=millis();
 if ((prev_pot1_percentage!=pot1_percentage_debounce)||(prev_pot2_percentage!=pot2_percentage_debounce)||(prev_pot3_percentage!=pot3_percentage_debounce)||(prev_pot4_percentage!=pot4_percentage_debounce)){
  char volumes[160];
  strcpy(volumes,pot1_percentage_str);
  strcat(volumes," ");
  strcat(volumes,pot2_percentage_str);
   strcat(volumes," ");
  strcat(volumes,pot3_percentage_str);
   strcat(volumes," ");
  strcat(volumes,pot4_percentage_str);
Serial.println(volumes);



  }
   prev_pot1_percentage=pot1_percentage_debounce;
  prev_pot2_percentage=pot2_percentage_debounce;
   prev_pot3_percentage=pot3_percentage_debounce;
  prev_pot4_percentage=pot4_percentage_debounce;
}

Serial.flush();
  while (Serial.available()) {
    
    // get the new byte:
    char inChar = (char)Serial.read();
    if (inChar=='!')Serial.println("!");
   
}
}





  
 


int scale(int pot){
  
 pot=floor((double)pot/0.98);
 pot=min(pot,100);
 pot=max(pot,0);
return pot;
}
int calc_percentage(int pot,int prev){
 if ((abs(pot-prev)>=thresh)){
//  Serial.print("pot value ");
//    Serial.println(pot);
//      Serial.print("prev pot value ");
//    Serial.println(prev);
//      Serial.print("entry gate ");
//    Serial.println((abs(pot-prev)));
 prev=pot;
   int pot_percentage=floor(((double)pot/(double)1023)*100);
   return pot_percentage;
}
else return NULL;
}
