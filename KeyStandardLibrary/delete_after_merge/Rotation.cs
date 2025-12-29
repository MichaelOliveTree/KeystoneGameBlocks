namespace Core.Types
{
    internal class Rotation
    {
        //    Public Structure MFRotation
        //        Private _items() As SFRotation

        //        Sub New(ByVal s As String)
        //            Dim sToks As Generic.List(Of String)
        //            sToks = Tokenizer.Tokenize(s)
        //            ReDim _items(sToks.Count \ 4 - 1)
        //            Dim j As Int32 = 0
        //            For k As Int32 = 0 To sToks.Count - 1 Step 4
        //                _items(j).rotation = New SFVec3f(CSng(sToks(k)), CSng(sToks(k + 1)), CSng(sToks(k + 2)))
        //                _items(j).angleRadians = CSng(sToks(k + 3))
        //                j += 1
        //            Next
        //        End Sub

        //        Sub New(ByVal vecs() As SFRotation)
        //            _items = vecs
        //        End Sub

        //        Default Public Property item(ByVal i As Integer) As SFRotation
        //            Get
        //                Return _items(i)
        //            End Get
        //            Set(ByVal value As SFRotation)
        //                _items(i) = value
        //            End Set
        //        End Property

        //        Public Function ToSFRotationArray() As SFRotation()
        //            Return _items
        //        End Function
        //        Public Function length() As Integer
        //            If _items Is Nothing Then
        //                Return 0
        //            Else
        //                Return _items.Length
        //            End If
        //        End Function
        //    End Structure

        //    Public Structure SFRotation
        //        Private _angleRadians As Single
        //        Private _rotation As SFVec3f

        //        Public Property angleRadians() As Single
        //            Get
        //                Return _angleRadians
        //            End Get
        //            Set(ByVal value As Single)
        //                _angleRadians = value
        //            End Set
        //        End Property
        //        Public Property rotation() As SFVec3f
        //            Get
        //                Return _rotation
        //            End Get
        //            Set(ByVal value As SFVec3f)
        //                _rotation = value
        //            End Set
        //        End Property
        //        Sub New(ByRef s As String)
        //            _rotation = New SFVec3f(s)
        //            _angleRadians = Tokenizer.ParseSingle(4, 4, s)
        //        End Sub
        //        Sub New(ByVal x As Single, ByVal y As Single, ByVal z As Single, ByVal length As Single)
        //            _rotation = New SFVec3f(x, y, z)
        //            _angleRadians = length
        //        End Sub
        //        Public Shared Function Inverse(ByRef r As SFRotation) As SFRotation
        //            Return New SFRotation(-r.rotation.x, -r.rotation.y, -r.rotation.z, -r.angleRadians)
        //        End Function
        //    End Structure
    }
}