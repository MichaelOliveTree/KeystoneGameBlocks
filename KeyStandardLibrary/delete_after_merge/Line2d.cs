using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Types
{
    internal class Line2d
    {
        //Public Structure Line2D
        //    Private _a, _b As PointF

        //    Public Sub New(ByVal a As PointF, ByVal b As PointF)
        //        _a = a
        //        _b = b
        //    End Sub

        //    Public Sub New(ByVal aX As Single, ByVal aY As Single, ByVal bX As Single, ByVal bY As Single)
        //        _a = New PointF(aX, aY)
        //        _b = New PointF(bX, bY)
        //    End Sub

        //    Public Property A() As PointF
        //        Get
        //            Return _a
        //        End Get
        //        Set(ByVal value As PointF)
        //            _a = value
        //        End Set
        //    End Property

        //    Public Property B() As PointF
        //        Get
        //            Return _b
        //        End Get
        //        Set(ByVal value As PointF)
        //            _b = value
        //        End Set
        //    End Property

        //    ''' <summary>
        //    ''' Returns the y value for a point on a line where we only know the x value
        //    ''' </summary>
        //    ''' <param name="line"></param>
        //    ''' <param name="x"></param>
        //    ''' <returns></returns>
        //    ''' <remarks></remarks>
        //    Public Shared Function FindPointGivenX(ByVal line As Line2D, ByVal x As Single) As Single

        //        ' find the slope of the line
        //        Dim m As Single = (line.B.Y - line.A.Y) / (line.B.X - line.A.X)
        //        Dim b As Single

        //        ' plug in one point of line into the slope intercept form of the line equation and solve for b
        //        'y = mx + b

        //        b = -m * line.A.X + line.A.Y

        //        ' now solve for the unknown y
        //        Return m * x + b
        //    End Function

        //    ''' <summary>
        //    ''' Returns the x value for a point on a line where we only know the y value
        //    ''' </summary>
        //    ''' <param name="line"></param>
        //    ''' <param name="y"></param>
        //    ''' <returns></returns>
        //    ''' <remarks></remarks>
        //    Public Shared Function FindPointGivenY(ByVal line As Line2D, ByVal y As Single) As Single
        //        ' find the slope of the line
        //        Dim m As Single = (line.B.Y - line.A.Y) / (line.B.X - line.A.X)
        //        Dim b As Single

        //        ' plug in one point of line into the slope intercept form of the line equation and solve for b
        //        'y = mx + b

        //        b = -m * line.A.X + line.A.Y

        //        ' now solve for the unknown x
        //        Return (y * m) - (m * b)
        //    End Function
        //End Structure
    }
}