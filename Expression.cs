//MIT License

//Copyright (c) 2020 Meinrad Recheis

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace PrimitiveCalculator
{
    public class Expression
    {
        public List<Operation> Operations = new List<Operation>();
        SimpleParser parser;  
        public bool MustConsumeClosingBracket;
        public double? Value;

        public Expression(string expression)
        {
            parser = new SimpleParser(expression.Trim());
        }

        public Expression(SimpleParser parser)
        {
            this.parser = parser;
        }

        public Expression(double nr)
        {
            Value = nr;
        }

        public double Eval()
        {
            var op_count = -1;
            while (parser.HasNext)
            {
                if (Operations.Count > op_count)
                    op_count = Operations.Count;
                else
                    break;
                parser.ConsumeAny(' ');
                if (parser.NextIs('('))
                {
                    parser.Skip(1);
                    var exp = new Expression(parser) { MustConsumeClosingBracket = true };
                    exp.Eval();
                    var op = new Operation { Operator = "+", Expression = exp };
                    Operations.Add(op);
                }
                else if (parser.NextIs("+-*^/%".ToCharArray()))
                {
                    var op = new Operation() { Operator = parser.NextChar.ToString() };
                    parser.Skip(1);
                    parser.ConsumeAny(' ');
                    if (parser.NextIs('('))
                    {
                        parser.Skip(1);
                        var exp = new Expression(parser);
                        exp.MustConsumeClosingBracket = true;
                        op.Expression = exp;
                        exp.Eval();
                    }
                    else
                        op.Expression = new Expression(ReadNumber(parser));
                    Operations.Add(op);
                }
                else if (parser.NextIs("0.123456789".ToCharArray()))
                {
                    Operations.Add(new Operation { Operator = "+", Expression = new Expression(ReadNumber(parser)) });
                }
                else if (parser.NextIs(')') && MustConsumeClosingBracket)
                {
                    parser.Skip(1);
                    break;
                }
            }
            if (!Operations.Any())
                return double.NaN;
            var first_op=Operations.First();
            if (string.IsNullOrEmpty(first_op.Operator))
                first_op.Operator = "+";
            else if (first_op.Operator == "-")
            {
                first_op.Operator = "+";
                first_op.Expression.Value = (first_op.Expression.Value ??Double.NaN) * (-1);
            }
            if (Operations.Count == 1)
            {
                var op = Operations[0];
                var val = op.Expression.Value;
                if (val == null)
                    return double.NaN;
                if (op.Operator == "-")
                    Value = (-1.0 * (val.Value));
                else
                    Value = val;
            }
            else if (Operations.Count == 2)
            {
                Value=Operations[1].Apply(Operations[0].Apply(0));
            }
            else
            {
                if (Precedence(Operations[0].Operator) > 0)
                    return double.NaN; // only + and minus may be first operator!
                // repeated contraction by precedence
                while (Operations.Count>1)
                {
                    var highest_op = Operations.Select(x => x.Operator).OrderByDescending(x => Precedence(x)).First();
                    if (Precedence(highest_op) == 0)
                    {
                        double sum = 0;
                        foreach (var op in Operations)
                            sum = op.Apply(sum);
                        return sum;
                    }
                    var ops = new List<Operation>();
                    int i = 0;
                    foreach (var op in Operations)
                    {
                        if (op.Operator == highest_op)
                        {
                            var last_op = ops[ops.Count-1];
                            last_op.Expression.Value=op.Apply(last_op.Expression.Value ?? double.NaN);
                            i++;
                            continue;
                        }
                        ops.Add(op);
                        i++;
                    }
                    Operations = ops;
                }
                return Operations[0].Apply(0);
            }
            return Value ?? double.NaN;
        }

        private double ReadNumber(SimpleParser parser)
        {
            var factor = parser.NextIs('-') ? -1 : 1;
            parser.ConsumeAny('+', '-');
            return factor * double.Parse(parser.ConsumeAny("0.123456789".ToCharArray()), CultureInfo.InvariantCulture);
        }

        private int Precedence(string op)
        {
            switch (op)
            {
                case "+":
                case "-":
                    return 0;
                case "*":
                case "/":
                case "%":
                    return 1;
                case "^":
                    return 2;
            }
            return -1;
        }
    }

    public class Operation
    {
        public string Operator;
        public Expression Expression;

        public double Apply(double v)
        {
            if (Expression == null)
                return double.NaN;
            switch(Operator)
            {
                case "+":
                    return v + Expression.Value ?? double.NaN;
                case "-":
                    return v - Expression.Value ?? double.NaN;
                case "*":
                    return v * Expression.Value ?? double.NaN;
                case "/":
                    return v / Expression.Value ?? double.NaN;
                case "%":
                    return v % Expression.Value ?? double.NaN;
                case "^":
                    return Math.Pow(v , Expression.Value ?? double.NaN);
            }
            return double.NaN;
        }
    }

    [TestFixture]
    public class CalculatorTests
    {
        [Test]
        public void SimpleTests()
        {
            new Expression("0").Eval().Should().Be(0);
            new Expression("1").Eval().Should().Be(1);
            new Expression("+1").Eval().Should().Be(1);
            new Expression("-1").Eval().Should().Be(-1);
            new Expression("0.07").Eval().Should().Be(0.07);
            new Expression("-1+1").Eval().Should().Be(0);
            new Expression("-1-2").Eval().Should().Be(-3);
            new Expression("123+246").Eval().Should().Be(369);
        }

        [Test]
        public void NestingTests()
        {
            new Expression("(1+2)*3").Eval().Should().Be(9);
            new Expression("1+(2*3)").Eval().Should().Be(7);
            new Expression("(77)").Eval().Should().Be(77);
            new Expression("-(77)").Eval().Should().Be(-77);
            new Expression("-(((77)))").Eval().Should().Be(-77);
            new Expression("-(-((77)))").Eval().Should().Be(77);
            new Expression("-(-(-(77)))").Eval().Should().Be(-77);
            new Expression("1+(1 +(1+(1)))").Eval().Should().Be(4);
            new Expression("5^(1+1)").Eval().Should().Be(25);
        }

        [Test]
        public void OperatorTests()
        {
            new Expression("5+2").Eval().Should().Be(7);
            new Expression("5-2").Eval().Should().Be(3);
            new Expression("2-5").Eval().Should().Be(-3);
            new Expression("5*2").Eval().Should().Be(10);
            new Expression("5%2").Eval().Should().Be(1);
            new Expression("2%5").Eval().Should().Be(2);
            new Expression("5/2").Eval().Should().Be(2.5);
            new Expression("2/5").Eval().Should().Be(0.4);
            new Expression("5^2").Eval().Should().Be(25);
            new Expression("2^5").Eval().Should().Be(32);
            new Expression("2*-2").Eval().Should().Be(-4);
            new Expression("-2*-2").Eval().Should().Be(4);
        }

        [Test]
        public void PrecedenceTests()
        {
            new Expression("1+1+1+1+1").Eval().Should().Be(5);
            new Expression("-1+1-1+1-1").Eval().Should().Be(-1);
            new Expression("1+2*3").Eval().Should().Be(7);
            new Expression("1*2+3").Eval().Should().Be(5);
            new Expression("1+2*2^3").Eval().Should().Be(17);
            new Expression("1^2+2*3").Eval().Should().Be(7);
            new Expression("1+2^2*3").Eval().Should().Be(13);
            new Expression("1*2*2*3").Eval().Should().Be(12);
            new Expression("1/2/2").Eval().Should().Be(0.25);
        }

        [Test]
        public void NegativeTests()
        {
            new Expression("").Eval().Should().Be(Double.NaN);
        }
    }
}
