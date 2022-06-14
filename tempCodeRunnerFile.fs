
let rec h x =             // Ill-typed: h not  polymorphic in its  own body  
      if true then 22                  
      else h 7 + h false      
