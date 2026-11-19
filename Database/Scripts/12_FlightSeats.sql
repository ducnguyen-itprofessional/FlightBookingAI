USE FlightBookingAI;
GO

CREATE TABLE FlightSeats
(
    FlightSeatID INT IDENTITY(1,1) PRIMARY KEY,

    FlightID INT NOT NULL,

    SeatNumber VARCHAR(5) NOT NULL,

    Status VARCHAR(20) NOT NULL
        CONSTRAINT DF_FlightSeats_Status
        DEFAULT 'Available',

    CONSTRAINT FK_FlightSeats_Flights
        FOREIGN KEY (FlightID)
        REFERENCES Flights(FlightID),

    CONSTRAINT UQ_FlightSeats_Flight_Seat
        UNIQUE (FlightID, SeatNumber),

    CONSTRAINT CK_FlightSeats_Status
        CHECK (Status IN ('Available', 'Held', 'Booked'))
);
GO
