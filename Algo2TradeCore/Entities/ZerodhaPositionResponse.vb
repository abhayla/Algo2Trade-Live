Imports KiteConnect

Namespace Entities
    Public Class ZerodhaPositionResponse
        Implements IPositionResponse

        Public Property WrappedPositionResponse As PositionResponse

        Public Property Day As List(Of IPosition) Implements IPositionResponse.Day

        Public Property Net As List(Of IPosition) Implements IPositionResponse.Net

    End Class
End Namespace