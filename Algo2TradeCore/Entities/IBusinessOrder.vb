Namespace Entities
    Public Interface IBusinessOrder
        Property ParentOrderIdentifier As String
        Property ParentOrder As IOrder
        Property TargetOrder As IEnumerable(Of IOrder)
        Property SLOrder As IEnumerable(Of IOrder)
        Property AllOrder As IEnumerable(Of IOrder)
        'Property SignalCandle As IPayload
    End Interface
End Namespace
