Namespace Entities
    Public Class Field
        Public Sub New(ByVal fieldType As TypeOfField)
            Me.FieldType = fieldType
        End Sub
        Public Sub New(ByVal fieldType As TypeOfField, ByVal value As Object)
            Me.FieldType = fieldType
            Me.Value = value
        End Sub
        Public ReadOnly Property FieldType As TypeOfField
        Public Property Value As Object
    End Class
End Namespace