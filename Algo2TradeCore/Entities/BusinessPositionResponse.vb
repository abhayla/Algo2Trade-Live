Imports KiteConnect

Namespace Entities
    Public Class BusinessPositionResponse
        Implements IPositionResponse

        Public Property Day As List(Of IPosition) Implements IPositionResponse.Day

        Public Property Net As List(Of IPosition) Implements IPositionResponse.Net

    End Class
End Namespace