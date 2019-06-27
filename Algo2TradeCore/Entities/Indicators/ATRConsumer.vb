Namespace Entities.Indicators
    Public Class ATRConsumer
        Inherits PayloadToIndicatorConsumer
        Public ReadOnly Property ATRPeriod As Integer
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer, ByVal atrPeriod As Integer)
            MyBase.New(associatedParentConsumer)
            Me.ATRPeriod = atrPeriod
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}({2})", Me.ParentConsumer.ToString, Me.GetType.Name, Me.ATRPeriod)
        End Function
        Class ATRPayload
            Implements IPayload
            Public Sub New()
                Me.ATR = New Field(TypeOfField.ATR)
            End Sub
            Public Property TradingSymbol As String Implements IPayload.TradingSymbol
            Public Property ATR As Field
            Public Overrides Function ToString() As String
                Return String.Format("ATR:{0}", Math.Round(Me.ATR.Value, 4))
            End Function
        End Class
    End Class
End Namespace