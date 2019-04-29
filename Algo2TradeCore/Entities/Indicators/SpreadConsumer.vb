Namespace Entities.Indicators
    Public Class SpreadConsumer
        Inherits PayloadToIndicatorConsumer
        Public ReadOnly Property FarSpreadField As Enums.TypeOfField
        Public ReadOnly Property NearSpreadField As Enums.TypeOfField
        Public ReadOnly Property AnotherParentConsumer As IPayloadConsumer
        Public Sub New(ByVal associatedFarParentConsumer As IPayloadConsumer,
                       ByVal associatedNearParentConsumer As IPayloadConsumer,
                       ByVal farSpreadField As Enums.TypeOfField,
                       ByVal nearSpreadField As Enums.TypeOfField)
            MyBase.New(associatedFarParentConsumer)
            Me.AnotherParentConsumer = associatedNearParentConsumer
            Me.FarSpreadField = farSpreadField
            Me.NearSpreadField = nearSpreadField
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}({2},{3})", Me.ParentConsumer.ToString, Me.GetType.Name, Me.FarSpreadField.ToString, Me.NearSpreadField.ToString)
        End Function
        Class SpreadPayload
            Implements IPayload
            Public Sub New()
                Me.Spread = New Field(TypeOfField.Spread)
            End Sub
            Public Property TradingSymbol As String Implements IPayload.TradingSymbol
            Public Property Spread As Field
        End Class
    End Class
End Namespace