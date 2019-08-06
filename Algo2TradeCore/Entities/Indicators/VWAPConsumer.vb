Namespace Entities.Indicators
    Public Class VWAPConsumer
        Inherits PayloadToIndicatorConsumer
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer)
            MyBase.New(associatedParentConsumer)
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}", Me.ParentConsumer.ToString, Me.GetType.Name)
        End Function
        Class VWAPPayload
            Implements IPayload
            Public Sub New()
                Me.VWAP = New Field(TypeOfField.VWAP)
            End Sub
            Public Property TradingSymbol As String Implements IPayload.TradingSymbol
            Public Property VWAP As Field
            Public Overrides Function ToString() As String
                Return String.Format("VWAP:{0}", Math.Round(Me.VWAP.Value, 4))
            End Function
        End Class
    End Class
End Namespace