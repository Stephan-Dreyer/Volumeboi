#include <stdio.h>
#include <stdlib.h>
#include <string.h>
// constants
#define thresh 10
#define alpha 0.2
//funciton defs
int calc_percentage(int pot, int* prev);
int scale(int pot);

// globlas
int prev_pot1 = 0;
int prev_pot2 = 0;
int prev_pot3 = 0;
//int prev_pot4 = 0;
int pot1_percentage;
int pot2_percentage;
int pot3_percentage;
//int pot4_percentage;
int prev_pot1_percentage = 0;
int prev_pot2_percentage = 0;
int prev_pot3_percentage = 0;
//int prev_pot4_percentage = 0;
char pot1_percentage_str[sizeof(char) * 4];
char pot2_percentage_str[sizeof(char) * 4];
char pot3_percentage_str[sizeof(char) * 4];
//char pot4_percentage_str[sizeof(char) * 4];
unsigned long now;
void setup() {
  Serial.begin(112500);
  pinMode(A0, INPUT);
  pinMode(A1, INPUT);
  pinMode(A2, INPUT);
  //pinMode(A7,INPUT);
  now = millis();
}

void loop() {

  int pot1 = analogRead(A0);
  int pot2 = analogRead(A1);
  int pot3 = analogRead(A2);
  //int pot4=analogRead(A7);


  int pot1_debounce = (alpha * pot1) + ((1 - alpha) * prev_pot1);
  int pot2_debounce = (alpha * pot2) + ((1 - alpha) * prev_pot2);
  int pot3_debounce = (alpha * pot3) + ((1 - alpha) * prev_pot3);
  //int pot4_debounce=(alpha*pot4)+((1-alpha)*prev_pot4);

  Serial.println(pot2_debounce);


  pot1_percentage = calc_percentage(pot1_debounce,& prev_pot1);
  pot2_percentage = calc_percentage(pot2_debounce,& prev_pot2);
  pot3_percentage = calc_percentage(pot3_debounce,& prev_pot3);
  //pot4_percentage=calc_percentage(pot4_debounce,&prev_pot4); 

  

  pot1_percentage = scale(pot1_percentage);
  pot2_percentage = scale(pot2_percentage);
  pot3_percentage = scale(pot3_percentage);
  //pot4_percentage=scale(pot4_percentage);




  itoa(pot1_percentage, pot1_percentage_str, 10);
  itoa(pot2_percentage, pot2_percentage_str, 10);
  itoa(pot3_percentage, pot3_percentage_str, 10);
  //itoa(pot4_percentage,pot4_percentage_str,10);




  if ((millis() - now) >= 130) {
    now = millis();
    if (
      (prev_pot1_percentage != pot1_percentage) 
      || (prev_pot2_percentage != pot2_percentage) 
      || (prev_pot3_percentage != pot3_percentage) 
      //|| (prev_pot4_percentage != pot4_percentage)
      ) {
      char volumes[160];
      strcpy(volumes, pot1_percentage_str);
      strcat(volumes, " ");
      strcat(volumes, pot2_percentage_str);
      strcat(volumes, " ");
      strcat(volumes, pot3_percentage_str);
      //strcat(volumes, " ");
      //strcat(volumes, pot4_percentage_str);
      Serial.println(volumes);



    }
    prev_pot1_percentage = pot1_percentage;
    prev_pot2_percentage = pot2_percentage;
    prev_pot3_percentage = pot3_percentage;
    //prev_pot4_percentage = pot4_percentage;
  }

  Serial.flush();
  while (Serial.available()) {

    // get the new byte:
    char inChar = (char)Serial.read();
    if (inChar == '!') Serial.println("!");

  }
}









int scale(int pot){

  pot = floor((double)pot / 0.99);
  pot = min(pot, 100);
  pot = max(pot, 0);
  return pot;
}

int calc_percentage(int pot, int* prev){
 *prev=pot;
  int pot_percentage = floor(((double)pot / (double)4095) * 100);
  return pot_percentage;
}
