Namespace Entities.Indicators
    Public Class SpreadRatioConsumer
        Inherits PayloadToIndicatorConsumer
        Public ReadOnly Property SpreadRatioField As Enums.TypeOfField
        Public Property HigherContract As OHLCPayload
        Public Property LowerContract As OHLCPayload
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer,
                       ByVal spreadRatioField As Enums.TypeOfField)
            MyBase.New(associatedParentConsumer)
            Me.SpreadRatioField = spreadRatioField
            If Not Me.ParentConsumer.TypeOfConsumer = IPayloadConsumer.ConsumerType.Pair Then
                Throw New ApplicationException("Spread cannot be calculated. Parent consumer of a spread consumer should be a Pair Consumer")
            End If
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}({2})", Me.ParentConsumer.ToString, Me.GetType.Name, Me.SpreadRatioField.ToString)
        End Function
        Class SpreadRatioPayload
            Implements IPayload
            Public Sub New()
                Me.Spread = New Field(TypeOfField.Spread)
                Me.Ratio = New Field(TypeOfField.Ratio)
            End Sub
            Public Property TradingSymbol As String Implements IPayload.TradingSymbol
            Public Property Spread As Field
            Public Property Ratio As Field
            Public Overrides Function ToString() As String
                Return String.Format("Spread:{0}, Ratio:{1}", Me.Spread.Value, Me.Ratio.Value)
            End Function
        End Class
    End Class
End Namespace