Namespace Entities.Indicators
    Public Class RSIConsumer
        Inherits PayloadToIndicatorConsumer
        Public ReadOnly Property RSIPeriod As Integer
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer, ByVal rsiPeriod As Integer)
            MyBase.New(associatedParentConsumer)
            Me.RSIPeriod = rsiPeriod
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}({2})", Me.ParentConsumer.ToString, Me.GetType.Name, Me.RSIPeriod)
        End Function
        Class RSIPayload
            Implements IPayload
            Public Sub New()
                Me.RSI = New Field(TypeOfField.RSI)
            End Sub
            Public Property TradingSymbol As String Implements IPayload.TradingSymbol
            Public Property RSI As Field
            Public Property AverageGain As Decimal
            Public Property AverageLoss As Decimal
            Public Overrides Function ToString() As String
                Return String.Format("RSI:{0}", Math.Round(Me.RSI.Value, 4))
            End Function
        End Class
    End Class
End Namespace