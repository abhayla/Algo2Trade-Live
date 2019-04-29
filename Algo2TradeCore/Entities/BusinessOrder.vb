Namespace Entities
    Public Class BusinessOrder
        Implements IBusinessOrder

        Public Property ParentOrder As IOrder Implements IBusinessOrder.ParentOrder

        Public Property ParentOrderIdentifier As String Implements IBusinessOrder.ParentOrderIdentifier

        Public Property SLOrder As IEnumerable(Of IOrder) Implements IBusinessOrder.SLOrder

        Public Property TargetOrder As IEnumerable(Of IOrder) Implements IBusinessOrder.TargetOrder

        Public Property AllOrder As IEnumerable(Of IOrder) Implements IBusinessOrder.AllOrder

        'Public Property SignalCandle As IPayload Implements IBusinessOrder.SignalCandle

    End Class
End Namespace
