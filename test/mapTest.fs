let sample = Map [ (1, "a"); (2, "b") ]
 
sample.Add (3, "c") // evaluates to map [(1, "a"); (2, "b"); (3, "c")]
sample.Add (2, "aa") // evaluates to map [(1, "a"); (2, "aa")]