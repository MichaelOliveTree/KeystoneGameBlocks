using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Types
{
    // todo: should a vertex track the face it's in?
    public class Vertex
    {
        private Vector3f _vector;
        private Vector3f _normal;
        //private Vector2f _textureCoord;


    }

    //    Public Class SFVertex
    //        Private _point As SFVec3f
    //        Private _normal As SFVec3f
    //        Private _uv As SFVec2f 'vertex normal
    //        Private _color As SFColor
    //        Private _colorSpecified As Boolean
    //        Private _normalSpecified As Boolean
    //        Private _uvSpecified As Boolean
    //        Private _index As Int32
    //        Private _indexSpecified As Boolean

    //        Public ReadOnly Property isIndexed() As Boolean
    //            Get
    //                Return _indexSpecified
    //            End Get
    //        End Property
    //        Public Property index() As Int32
    //            Get
    //                Return _index
    //            End Get
    //            Set(ByVal value As Int32)
    //                _index = value
    //                _indexSpecified = DirectCast(IIf(value >= 0, True, False), Boolean)
    //            End Set
    //        End Property

    //        Public Property point() As SFVec3f
    //            Get
    //                Return _point
    //            End Get
    //            Set(ByVal value As SFVec3f)
    //                _point = value
    //            End Set
    //        End Property
    //        Public Property normal() As SFVec3f
    //            Get
    //                Return _normal
    //            End Get
    //            Set(ByVal value As SFVec3f)
    //                _normal = value
    //                _normalSpecified = DirectCast(IIf(value.x = 0 AndAlso value.y = 0 AndAlso value.z = 0, False, True), Boolean)
    //            End Set
    //        End Property
    //        Public Property uv() As SFVec2f
    //            Get
    //                Return _uv
    //            End Get
    //            Set(ByVal value As SFVec2f)
    //                _uv = value
    //                _uvSpecified = DirectCast(IIf(value.x = 0 AndAlso value.y = 0, False, True), Boolean)
    //            End Set
    //        End Property
    //        Public Property color() As SFColor
    //            Get
    //                Return _color
    //            End Get
    //            Set(ByVal value As SFColor)
    //                _color = value
    //                _colorSpecified = DirectCast(IIf(_color.r = 0 AndAlso _color.g = 0 AndAlso _color.b = 0, False, True), Boolean)
    //            End Set
    //        End Property
    //        Public ReadOnly Property normalSpecified() As Boolean
    //            Get
    //                Return _normalSpecified
    //            End Get
    //        End Property
    //        Public ReadOnly Property uvSpecified() As Boolean
    //            Get
    //                Return _uvSpecified
    //            End Get
    //        End Property
    //        Public Shared Operator =(ByVal v1 As SFVertex, ByVal v2 As SFVertex) As Boolean
    //            Return CBool(IIf(v1.point = v2.point AndAlso v1.normal = v2.normal AndAlso v1.color = v2.color AndAlso v1.uv = v2.uv, True, False))
    //        End Operator
    //        Public Shared Operator <>(ByVal v1 As SFVertex, ByVal v2 As SFVertex) As Boolean
    //            Return Not v1 = v2
    //        End Operator
    //    End Class
}
