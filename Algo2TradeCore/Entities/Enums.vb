Namespace Entities
    Public Module Enums
        Enum APISource
            Zerodha = 1
            Upstox
            None
        End Enum
        Enum TypeOfExchage
            NSE = 1
            MCX
            CDS
            None
        End Enum
        Enum TypeOfField
            Open
            Low
            High
            Close
            Volume
            SMA
            EMA
            ATR
            Supertrend
            Bollinger
            Spread
            Ratio
        End Enum
        Enum CrossDirection
            Above = 1
            Below
        End Enum
        Public Enum Position
            Above = 1
            Below
        End Enum
    End Module
End Namespace