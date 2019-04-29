Imports Algo2TradeCore.Entities
Imports Algo2TradeCore.Entities.UserSettings
Imports Utilities.Strings.StringManipulation

Module Common
    Public Const MASTER_KEY = "JOYMA"
    Public Const LOGIN_PENDING = "Login pending"
    Public Function IsZerodhaUserDetailsPopulated(ByVal userInputs As ControllerUserInputs) As Boolean
        If userInputs IsNot Nothing AndAlso userInputs.UserDetails IsNot Nothing Then
            Return userInputs.UserDetails.UserId IsNot Nothing AndAlso userInputs.UserDetails.UserId.Trim.Count > 0 AndAlso
                userInputs.UserDetails.Password IsNot Nothing AndAlso userInputs.UserDetails.Password.Trim.Count > 0 AndAlso
                userInputs.UserDetails.APIKey IsNot Nothing AndAlso userInputs.UserDetails.APIKey.Trim.Count > 0 AndAlso
                userInputs.UserDetails.APISecret IsNot Nothing AndAlso userInputs.UserDetails.APISecret.Trim.Count > 0 AndAlso
                userInputs.UserDetails.API2FAPin IsNot Nothing AndAlso userInputs.UserDetails.API2FAPin.Trim.Count > 0
        Else
            Return False
        End If
    End Function
    Public Function GetZerodhaCredentialsFromSettings(ByVal userInputs As ControllerUserInputs) As ZerodhaUser
        If userInputs IsNot Nothing AndAlso userInputs.UserDetails IsNot Nothing Then
            Return New ZerodhaUser With
               {.UserId = userInputs.UserDetails.UserId,
                .Password = Decrypt(userInputs.UserDetails.Password, MASTER_KEY),
                .APIVersion = "3",
                .APIKey = Decrypt(userInputs.UserDetails.APIKey, MASTER_KEY),
                .APISecret = Decrypt(userInputs.UserDetails.APISecret, MASTER_KEY),
                .API2FAPin = Decrypt(userInputs.UserDetails.API2FAPin, MASTER_KEY)}
        Else
            Return Nothing
        End If
    End Function
End Module
