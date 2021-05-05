# Neural Network Demo
A basic simulation of a Neural Network with one input, one output, and one hidden layer.
This takes in a .csv file with inputs between 0 and 1, with their expected outputs. Then it outputs the actual outputs to the .csv file.

The target equation is:
```csharp
f : [0, 1] => f(x) = sin(x)
```

The neural network\'s equation is a sigmoid:
```csharp
     x
-----------
  _________
\/1 + (x*x)
```

# Opening the Project
This project is compatible with Unity versions 2020.2 and upwards.

# How to Use
- On the 'Main Controls' screen, click \'Run Network\'.
- You can click on elements to the left, and then click \'Remove Element\' to remove them.
- You can add elements by clicking \'Add Element\'.
- You can automatically solve all expected outputs by clicking \'Solve Inputs\'
- You can manually edit each element\'s input and expected output. It will save to file if \'Auto-Save\' is on.
- When the graph is made, you can hover over a node to see its Input (X) vs Output (Y).

# Notes
- By default, there is no data. Opening the executable will give the option to create basic data.
- Data is stored at \'_Data\neural_data.csv\'.
- You can change the Neural Network settings in \'Options\'.
- Auto-Save is on by default. You can change this in \'Options\'.