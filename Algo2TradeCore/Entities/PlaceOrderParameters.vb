Imports Algo2TradeCore.Adapter

Namespace Entities
    Public Class PlaceOrderParameters
        Public Sub New(ByVal signalCandle As OHLCPayload)
            Me.SignalCandle = signalCandle
            Me.Supporting = New List(Of Object)
        End Sub

        Public Property EntryDirection As IOrder.TypeOfTransaction = IOrder.TypeOfTransaction.None
        Public Property Quantity As Integer = 0
        Public Property Price As Decimal = Decimal.MinValue
        Public Property TriggerPrice As Decimal = Decimal.MinValue
        Public Property SquareOffValue As Decimal = Decimal.MinValue
        Public Property StoplossValue As Decimal = Decimal.MinValue
        Public Property SignalCandle As OHLCPayload = Nothing
        Public Property OrderType As IOrder.TypeOfOrder = IOrder.TypeOfOrder.None
        Public Property Supporting As List(Of Object) = Nothing
        Public Property GenerateDifferentTag As Boolean = False
        Public Overrides Function ToString() As String
            Return String.Format("{0}{1}{2}{3}{4}{5}", EntryDirection.ToString(), Price, TriggerPrice, SquareOffValue, StoplossValue, If(SignalCandle Is Nothing, "Nothing", SignalCandle.SnapshotDateTime.ToString()))
        End Function
    End Class
End Namespace