using System;
using System.Diagnostics;
using Core.Converters;
using MTV3D65;

namespace Core.Types
{
    public struct Color
    {
        public float r, g, b, a;

        public Color(TV_COLOR tvColor) : this(tvColor.r, tvColor.g, tvColor.b, tvColor.a)
        {
        }

        /// <summary>
        /// values should all be between 0 and 1f
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="alpha"></param>
        public Color(float red, float green, float blue, float alpha)
        {
            Trace.Assert(red <= 1.0);
            Trace.Assert(green <= 1.0);
            Trace.Assert(blue <= 1.0);
            Trace.Assert(alpha <= 1.0);
            r = red;
            g = green;
            b = blue;
            a = alpha;
        }

        public static Color Parse(string s)
        {
            if (string.IsNullOrEmpty(s)) throw new ArgumentNullException();
            char[] splitter = {',', ';', ' '};
            string[] values = s.Split(splitter);
            float r = float.Parse(values[0]);
                // Tokenizer.ParseFloat(1, 4, s); //todo: tokenizing all 4 at once is better. parsesingle should only be used when needed to be done once
            float g = float.Parse(values[1]); //Tokenizer.ParseFloat(2, 4, s);
            float b = float.Parse(values[2]); //Tokenizer.ParseFloat(3, 4, s);
            float a = float.Parse(values[3]); //Tokenizer.ParseFloat(4, 4, s);
            return new Color(r, g, b, a);
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3}", r.ToString(), g.ToString(), b.ToString(), a.ToString());
        }

        /// <summary>
        /// RGBA
        /// </summary>
        /// <returns></returns>
        public int ToInt32()
        {
            return Core._Core.Globals.RGBA(r, g, b, a);
        }
    }
}

//Public Structure MFColor
//        Private _colors() As SFColor

//        Sub New(ByRef s As String)
//            Dim sToks As Generic.List(Of String)
//            sToks = Tokenizer.Tokenize(s)
//            ReDim _colors(sToks.Count \ 3 - 1)
//            Dim j As Int32 = 0
//            For k As Int32 = 0 To sToks.Count - 1 Step 3
//                _colors(j).r = CSng(sToks(k))
//                _colors(j).g = CSng(sToks(k + 1))
//                _colors(j).b = CSng(sToks(k + 2))
//                j += 1
//            Next
//        End Sub

//        Sub New(ByVal colors() As SFColor)
//            _colors = colors
//        End Sub

//        Default Public Property item(ByVal i As Integer) As SFColor
//            Get
//                Return _colors(i)
//            End Get
//            Set(ByVal value As SFColor)
//                _colors(i) = value
//            End Set
//        End Property

//        Public Function ToSFColorfArray() As SFColor()
//            Return _colors
//        End Function
//        Public Function length() As Integer
//            Return DirectCast(IIf(_colors Is Nothing, 0%, _colors.Length), Int32)
//        End Function
//    End Structure

//    Public Structure SFColor
//        Private _r, _g, _b As Single
//        Sub New(ByVal red As Single, ByVal green As Single, ByVal blue As Single)
//            _r = red : _g = green : _b = blue
//        End Sub
//        Sub New(ByRef s As String)
//            ' todo: i dont even have proper exception handling :(
//            If s <> "" Then
//                _r = Tokenizer.ParseSingle(1, 3, s)
//                _g = Tokenizer.ParseSingle(2, 3, s)
//                _b = Tokenizer.ParseSingle(3, 3, s)
//            End If
//        End Sub
//        Public Property r() As Single
//            Get
//                Return _r
//            End Get
//            Set(ByVal value As Single)
//                _r = value
//            End Set
//        End Property
//        Public Property g() As Single
//            Get
//                Return _g
//            End Get
//            Set(ByVal value As Single)
//                _g = value
//            End Set
//        End Property
//        Public Property b() As Single
//            Get
//                Return _b
//            End Get
//            Set(ByVal value As Single)
//                _b = value
//            End Set
//        End Property
//        Public Shared Operator =(ByVal c1 As SFColor, ByVal c2 As SFColor) As Boolean
//            Return CBool(IIf(c1.r = c2.r AndAlso c1.g = c2.g AndAlso c1.b = c2.b, True, False))
//        End Operator
//        Public Shared Operator <>(ByVal c1 As SFColor, ByVal c2 As SFColor) As Boolean
//            Return Not c1 = c2
//        End Operator
//    End Structure

//    Public Structure MFColorRGBA
//        Private _colors() As SFColorRGBA

//        Sub New(ByRef s As String)
//            Dim sToks As Generic.List(Of String)
//            sToks = Tokenizer.Tokenize(s)
//            ReDim _colors(sToks.Count \ 3 - 1)
//            Dim j As Int32 = 0
//            For k As Int32 = 0 To sToks.Count - 1 Step 4
//                _colors(j).r = CSng(sToks(k))
//                _colors(j).g = CSng(sToks(k + 1))
//                _colors(j).b = CSng(sToks(k + 2))
//                _colors(j).a = CSng(sToks(k + 3))
//                j += 1
//            Next
//        End Sub

//        Sub New(ByVal colors() As SFColorRGBA)
//            _colors = colors
//        End Sub

//        Default Public Property item(ByVal i As Integer) As SFColorRGBA
//            Get
//                Return _colors(i)
//            End Get
//            Set(ByVal value As SFColorRGBA)
//                _colors(i) = value
//            End Set
//        End Property

//        Public Function ToSFColorfArray() As SFColorRGBA()
//            Return _colors
//        End Function
//        Public Function length() As Integer
//            If _colors Is Nothing Then
//                Return 0
//            Else
//                Return _colors.Length
//            End If
//        End Function
//    End Structure