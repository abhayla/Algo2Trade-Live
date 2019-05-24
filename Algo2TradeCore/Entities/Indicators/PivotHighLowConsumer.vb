Namespace Entities.Indicators
    Public Class PivotHighLowConsumer
        Inherits PayloadToIndicatorConsumer
        Public ReadOnly Property Strict As Boolean
        Public Sub New(ByVal associatedParentConsumer As IPayloadConsumer, ByVal strict As Boolean)
            MyBase.New(associatedParentConsumer)
            Me.Strict = strict
        End Sub
        Public Overrides Function ToString() As String
            Return String.Format("{0}_{1}({2})", Me.ParentConsumer.ToString, Me.GetType.Name, Me.Strict)
        End Function
        Class PivotHighLowPayload
            Implements IPayload
            Public Sub New()
                Me.PivotHigh = New Field(TypeOfField.PivotHigh)
                Me.PivotLow = New Field(TypeOfField.PivotLow)
            End Sub
            Public Property TradingSymbol As String Implements IPayload.TradingSymbol
            Public Property PivotHigh As Field
            Public Property PivotHighSignalCandle As OHLCPayload
            Public Property PivotLow As Field
            Public Property PivotLowSignalCandle As OHLCPayload
            Public Overrides Function ToString() As String
                Return String.Format("Pivot High:{0}, Pivot High Signal Candle:{1}, Pivot Low:{2}, Pivot Low Signal Candle:{3}",
                                     Me.PivotHigh.Value, If(Me.PivotHighSignalCandle Is Nothing, "Nothing", Me.PivotHighSignalCandle.SnapshotDateTime.ToString),
                                     Me.PivotLow.Value, If(Me.PivotLowSignalCandle Is Nothing, "Nothing", Me.PivotLowSignalCandle.SnapshotDateTime.ToString))
            End Function
        End Class
    End Class
End Namespace