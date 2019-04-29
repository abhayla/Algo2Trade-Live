Namespace Entities
    Public Interface IUser
        Property UserId As String
        Property Password As String
        Property APISecret As String
        Property APIKey As String
        Property APIVersion As String
        Property API2FAPin As String
        Property DaysStartingCapitals As Dictionary(Of Enums.TypeOfExchage, Decimal)
        ReadOnly Property Broker As APISource
    End Interface
End Namespace
