# Primitive Calculator Math Expression Validator

This primitive calculator can evaluate math expressions containing the operations found on most primitive calculator app: + - * / % and ^.
It honors brackets ( ) and operator precedence. Bracket expressions can be as deep as your machine's stack allows (max recursion depth).

No C# compiler (like for instance Roslyn) has been used. The expression parser is hand-crafted and comes with a number of unit tests. 


## Interactive Console

```text
calc> 1/2*2
 = 1
calc> 2^1+1
 = 3
calc> 7777777777777+8888888888888
 = 16666666666665
calc> 1+2+3+4+5+7+8
 = 30
calc> 1+2*3
 = 7
calc> 1+2*3-1
 = 6
calc> (1+2)*(3-1)
 = 6
calc> (1+2)*(3-2)
 = 3
calc> 1+2*3-2
 = 5
calc> (1+2)*3
 = 9
calc> 44%10
 = 4
calc>  1+ 2 * 7
 = 15
calc> 10/3
 = 3.3333333333333335
calc> 0.3*9
 = 2.6999999999999997
calc> -0.22345 + -1.1
 = -1.32345
calc>
```