using Device.FingerPrint.TransactionAPI.Helpers;
using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Queue;
using Device.FingerPrint.TransactionAPI.Services;
using Fanex.Logging;
using FluentAssertions;
using Moq;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Services
{
    public class LinkingServiceTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly Mock<IRedisDatabase> _mockRedisDatabase;
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly Mock<IBatch> _mockBatch;
        private readonly Mock<IMassTransitProducer> _mockMassTransitProducer;

        private const string BASE64_KEY = "wFKfVDGZqRbyYCBi6IveMaQAh1ntWcP8ujT/U7r2xXs+J0L=4d3zEHSo5glN9kOpm";

        private readonly LinkingService _linkingService;

        private readonly APFingerprintModel fp4MockData = new APFingerprintModel()
        {
            Browser = "P3RTWrkoWSa3erD01vqlqUCxWrk01vqJqrkzerD01vqlq7cXtrI=coYTyKRHWSa36Qc7t26TBTRCtoXXtGdjyzMLYKwxaS7L1GkoW3FBaKwdYK54B3FAnQ5SCfJuPfhEbvFFWZFJ1ac7hU0XcK9HYzWLYzhubV0qaVHYyKFJnQ07qVc7hS0=bvFfnZR=tQM=YeYgy/wLYK54qDCj1rD3nv9HYzWLYzhTyKRS1QgUtoqTBTRZtSk2tGMuvQg/yTqJq217trI=WU1JhA1=W2YTB7JT62Rjtr6lISk=1Sd7qVCxWrk01vqJqUR3hQgUBUCxWrk0nAa0qT4TMGdjcG1=WrElaS7L1GkoW3qJq717trI=W/XZtSk2tGMuvQg/yTqJqUaL1S7L1eXKtG7Ln3qJq7aFBUH=Pr7JtGV=Cv54qKjAnQgUtoczqVgMqfV4y/wNqDcXt/hEB3F5C/6XqVD4WGd7aSaTvS7EyzMzC35zCTwxvEjMeM4JqGdXnSMuISa/nS9XqVCxWrk01v9dYzULYK54y/wuMSDrhARXyzMzC35zCTRcyKR4tZa2nQgzq/XtP3RLhQH7q/xTMVIGqD1X1Ac7WTqJqrI7WSC3nAFEnQkLq/xTMGk3cGDTtGMuIGk/cQH7t26uIrk3tQDEqT4T1r7J1QgjtQMTBTRXt2I7WrgjtKH41Gh0cr77cSa3q2EJP3RLhQH7q/xT6Sj3tSH7qDFVITFQnQao1AqTyKRU1AC/Wr74cG7=tTqlq7F=W2Ijhrd7qVI=hoa01QgEqV1=WrHjcKqJqr1XtGaLhQH7q/xTnQgE1ARLhQ40WGIryA1X1Ac7WTRkyZJTtrD01vqlqUCxWrk0nAa0qDFVITFQnQao1AqTyKRU1AC/Wr74cG7=tTqlq7F=W2Ijhrd7qVI=hoa01QgEqV1=WrHjcKqJqr1XtGaLhQH7q/xTnQgE1ARLhQ40WGIryA1X1Ac7WTRkyZJTtrD01vqlqUHXhoR=WSkrcKFD1Gc7qDFVITFQnQao1AqTyKRU1AC/Wr74cG7=tTqlq7F=W2Ijhrd7qVI=hoa01QgEqV1=WrHjcKqJqr1XtGaLhQH7q/xTnQgE1ARLhQ40WGIryA1X1Ac7WTRkyZJTtrD01vqlq7c7hU0XcKFTcQ7JcKHXtTF6IVhTyKRU1AC/Wr74cG7=tTqlq7F=W2Ijhrd7qVI=hoa01QgEqV1=WrHjcKqJqr1XtGaLhQH7q/xTnQgE1ARLhQ40WGIryA1X1Ac7WTRkAv4TWGIrar77cSa3IQgjhrd71KqlcZRH1AEm",
            Audio = "P3RjcQIXt3qlP3RjcQIXt3qlY/W3y/VzCzYECeh4CzV4BfWJqrg=nAC7aZ741vqlqrCJ1QDLq2EJqrDH1G7=6rDz1MdjcGaLhoUTB/wLYfDk",
            Hardware = "P3R4tGDE1rk3tvqlq7cXtrI=coYTyKR/WZaftGDzW3qlqTqJqrD3hSjXcGa/cZa31vqlq2u5CTqJqrkzhoFHq/xTaS7L1GkoW3FBaKwdYK54B3FAnQ5SCfJuPfhEqT4TnGD31ZcjWraftSg/cAR31Qg/PvqlYeqJqrI7cr7/1MH7tQk3PvqlBZEm",
            Preferences = "P3RJhQg2cQD21AYTB7JT1Q50aaYTyKR7tTRcyKREnQH7PrkL1vqlqUDznQV=6rDL1S0=n3qJq2I=cQCxMoa4WGk3cKql1rDJWSMJq2C7WoCXtSgecGk3hQc7q/XEW2a7yKR=WGaLIGDEhQRjWSMTBr1jtZC7yKRJtSCjtDCEtoRj1SMTB2I3cQMJqr7L1Ga51QIV6TqlcZRH1v4ThSk=nS77WEaLhQRJ1Q6TB2I3cQak"
        };

        private readonly APFingerprintModel fp5MockData = new APFingerprintModel()
        {
            Graphics = "P3R/hQgShAYTB2JT1r7L1Sa3WZRXt26TBTRr1eYzC/CTCTqJq2CEhAIHW3qlqrk+qT4TcS7UcGuTB/q4YK4TnGaX1SjEq/x3YfwJq2I7PZIKhAC7tG7L1vqlqrDJWGjjhraEnQYTyKRrtSgEq/xTYeI4PKFFWr7jtKqJqr1XtGdecZ7J1vqlq70=hrX7ho6u6SDLcrDzIoRj1G77t2IcqT4TWoI3tS07MoIgtGMTBTq/YfwSC/UgqT4TtG7L1acX1ZIxq/x3yKRJnQg76SD4q/xTh2aEcKqJqrdXtrabtS7Lq/xTtQ7E1AqTyKR0nAI7WUdXtQ7Eq/xdYK4TWSjj1GkoeS1rWSaEQKqlYK4TWSjj1GkoeS1rWSaEQvqlYK4TWSjj1Gko6rdHWTqlYK4TWSjj1Gko6SkJtoqTBTR31SRjbfwJqfwJqfwJqfwXqT4T1Sd=hrDJ6Qd4nGVTB/VJqrcJtSRjtVC=tAF=WS7E1Mk41ARjcG7=tTqlq2C=cAR/1vH=cra3q2EJq2c7hrcJ6rDznQCzq/XNq217trI=WTqlq7c7hU0XcKqJq2R7trI7Wra3q/xTaSaTvS7EqDc7hUcYqT4Tcra3WS7=tTqlq7c7hUcYqfVLYKwxeoF7tUcYqVaeqfqLYKFfnZR=tQ7HtvUTyKRznGDUnQg2eGDL1oaj1SaQ1ARznQkLq/xTaSaTIE4uIEdeeKFDM3wdy/wubVk41QgZeKFDM3FZeDCYqVaeqfVLYKFfnZR=tQ7HtvUTyKRHtrHjWS071D17trI=WTqlqUc=tScJ1vFRtrYLqKjRt2I7tKUTyKRHtrHjWS071DR7trI7Wra3q/xT6MgZeVMubV7LcGaJyKFRt2I7tKjvbvFavV6uIoRjWGjXhoYuC/Y4qKu4Pfw4YfwzIeU3bvFVnAR7ho6zIfVdqZ1zAza8YKF4WH9HAzwJqV6zIfVdbvqJqrHjPDI7PZIHWraenAX7q/xdC/Y5CK4TtQD5ar77coF=W2IVnQHzq/XNq/wTB/Y3CzhoyKqdq/xzY/WSCoEJqrHjPDR7trI7WrRH1r17W7CXPrMTB/VSYzuEyKRjtG7jWSaUeG7L1acX1ZIxMrDL1SMTB2JTYKqlYv4TYvqlYAEJqrDJnQDz1QI6tS7LcDCXPravhQg21vqlP3q4q/xdyKqdq/xdYfqE8v4TtQD56QgXWSkEWrk4PvqlYe1kyKRo1QR2tVa5cGaLWS7=t2YTB2JT1AjE1QgznQkLW3qlQ3RFeUcYIakXt2CEhQg/1QI8hAR3hA7zqT4TIajMASRJ1QgUASHXtrHjPKqJqUahaDk/tG74ASC=t2I3tS4TyKRDQDI8hSkJtoR8h2ar1ra3ASjjtG181rd=hA6TyKRDQDI81Ga4cGj8hSdjtAwTyKRDQDI81G7znrkXt2I8cG701AR8WAa7W2UTyKRDQDI81rd=hAI8hrd7tr6TyKRDQDI812Rj1HkU1AFEnKqJqUahaDk4tSdg1SkLASkr12C7cDk/tGD0WKqJqUahaDkznGDU1AR8cGa5cZa31akJtS6TyKRDQDI8cGa5cZa31ak/tSH4WrazWS7=t7kTWZI/qT4TIajMAoI7PZIHWra8hSk0WZR7WoCXtSg8WrcEh3qJqUahaDkE1AjEcAR7AS1XtZI7W7kjtr7ztoI3toFXh3qJqUahaDkE1AjEcAR7ASHXW2R=W7k/tGD0WDkEtHk71Gc7qT4TIajMAoCvIEqTyKRyvDR8WGD3hQdJ1Qd8WSjj1Ga3ASC=tAFXtGMTyKRiIaC81Qd7tQaLcDkXtrI7PDkHnQgEqT4TeEaeAS1TtHk31QgU1AR8tQ74tQD4qT4TeEaeAoCEhQgUhARUASI7Wr7ShAIXcrazqT4TeEaeAoI7PZIHWra81rd=hA6TyKRiIaC8cGa5cZa31akrtGkjcDkJnQg7hAqTyKRiIaC8cGa5cZa31akxhQdrAS1JtSDEqT4TeEaeAoI7PZIHWra8nGDJ17krtGkjcDkJnQg7hAqTyKRiIaC8cra3cGa5ASD3WrDgASkTnra/cKqJq7cD6UcYASRJ1QgUAS1HtrC81AjE1QgU1Q6TyKRAIMRZeDk/tSd=W7kTcQ1r1AR81rd=hA6TyKRAIMRZeDk/tSH4WrazWSaUAoI7PZIHWra8WzCEh3qJq7cD6UcYASC=tAF31ACz1QI8cGa5cZa31akzYoI/AoC31SqTyKRAIMRZeDkU1QRH1Hk31QgU1AR7W7kXtr1=qT4TaEaKIEd81GaTcQc8WSjj1Ga3W3qJq7cD6UcYASI7WZIxAoI7PZIHWrMTyKRAIMRZeDkUWrDoASRH1r17W2YTyKRAIMRZeDkJtoC7ASC=t2I7PZ6TyKRAIMRZeDk0cQdEnakUWrDoqT4TaEaKIEd8WGkJPQc=t7k0tSI7q7EJq2CHWZF=W2I71Va5cGaLWS7=t2YTB7JT6MgZeVa8nQgzcGDLhSaUASD3WrDgW3qJqUahaDkTtGaL1Dk0nQg0hAuTyKRDQDI8hSdXWDk/tSgEWrkJqT4TIajMASC=tGk3ASRH1r17W7kxhQdrAS1JtSDEqT4TIajMASI7WZIxASCJhQH4qT4TIajMASIXWSX=nQgEAoIXtQa3AoDH1ARgqT4TIajMAS1JtSDEASRJ1QgUqT4TIajMAS13hQc81Ga4cGuTyKRDQDI8WGkJPQc=t7k=1r1z1AI8hSdjtAwTyKRDQDI8WSjj1Ga3AoI7PZIHWra8tGkUqT4TIajMAoI7PZIHWra8hSk0WZR7WoCXtSg8h2FEh3qJqUahaDkE1AjEcAR7ASC=tAF31ACznQkLAoR2cGYTyKRDQDI8cGa5cZa31akrnQdE1AR8hQgXWSkEWrk4nQYTyKRDQDI8cGa5cZa31ak0nAR3toR8hSdjtAF8cGk81QI21vqJqUahaDkzMUcKqT4TvEjvAoFjWrDJtGaJAoCxhQI7W7k/tSH4nQd7qT4TeEaeASaJ1QH7t2I8nQgU1Aj8cQ7LcKqJqUkDMHkrhrk8WraL1Ga3ASHXWGHjWKqJqUkDMHkzcGDL1GD31DkU1ARXcrDEnA17W3qJqUkDMHkE1AjEcAR7AS1JtSDEqT4TeEaeAoI7PZIHWra81rd=hAI8tG7L1QD3qT4TeEaeAoI7PZIHWra8nGDJ17krtGkjcKqJqUkDMHkE1AjEcAR7ASjjtG181rd=hAI8tG7L1QD3qT4TeEaeAo17W2I7PDkjW2RjPak=hrX7ho6TyKRAIMRZeDkTtGaL1DkrcQg/ASa5cGaL1GaUqT4TaEaKIEd8hSkJtoR8h2ar1ra3AS1JtSDEqT4TaEaKIEd8hSk0WZR7WoC71DkE1AjEcAR7AoYzcGYTyKRAIMRZeDk/tSH4WrazWSaUAoI7PZIHWra8WzCEhHkzWrcTqT4TaEaKIEd81GaTcQc8WraL1Ga31AR8nQgrt3qJq7cD6UcYASI7h2a2AoCxhQI7W2YTyKRAIMRZeDkU1AFEnDkE1AjEcAR7qT4TaEaKIEd81ZRjcHkTcQ1r1ARzqT4TaEaKIEd8tGkz1ak/tSgE1AjEqT4TaEaKIEd8tAaJcG781ZRjc3qJq7cD6UcYAoF=tZ72tSg8tQkU1vRcyKR/tSH4WrazWSaUaGa5cZa31M1=WrHjcZYTB70cyKR0hAjVWrDo62ar1ra3W3qlBK4TtQD56SkJtoRFcZIjhSj01QgEW3qlBK4TtQD5ara3cGa56AIEWr7TW3qlYehJqrHjPD17W2I7PDaLnQ1=WrHQ1QCEtoRzq/xEYfUSyKR0hAjGWrD2tQaLcDaLnQ1=WrHQ1QCEtoRzq/xdYfqEyKR0hAjQhARgnQg2ara/cGk3W3qlYzFkyKRrtSgEW3qlP3RjcrDXtGDTtGaGtSgEW3qlQ3RFWr7jtKqJqUCjtG7TWrUTyKRfhQHTWr7jqT4T6SkLWSkJhAYTyKRftoa3nQa3qVg7c3qJqUc7toR2nQVTyKRq1QdS1AIXhSVTyKRMnQH7W3FB1AWuMrk0hQ5TyKRMWraTcQCx1A6ueaYTyKRQ1ARUhQgjqT4TMrkTtoI=qT4TerkEt3FehQgzqT4TMSa2tSMuaMUTyKReITF6Wr9TyKRah2aLcZMTAv4T1rkLcVdXWo6TB7JT6ARXhQ4TyKRfhQdXh2RXqT4T6SD0h2RXhvqJqUC=t2C=tGDzqT4T6SkHWr77WTFB1AWTyKRZ1Qk31S7jqT4TvGaJcraEnQCjqT4TaG701AYueraoqDR=tQDLqT4TaZR7h2a/nGaEqVHeqT4Tara31GDLhvqJq7R=hrkEt3qJqUg=cG9uMSDLW3qJq7C71Sk7qDaRqT4TMEhuMZR=qT4TaQRHt2IHq7EJqr1=t2IC1AI3nQCzq/XNqUD3nQDJq/XNq2cX1ZIxq/xdCfhLCzqSCeh3Cv4ThAC/1QgEq/xdYT4T1GazhSaLcKqlY34ThrDz1QdXtrMTB/V3yKRx1Q72nZ6TB/VH8v4T6SDJnQR3nvqlP3RonQIEnKqlYeYzy/uzCeUzCzMJqrDzhSaLcKqlYewJqrI7WSC7t26TB/YJqrRjWSaJnQg7q/xdYK4TnGaX1SjEq/xdYoEJqUCjtQR3nQVTB2JTcS7UcGuTB/VECT54CzudY/MJqrDzhSaLcKqlYewJqrI7WSC7t26TB/YJqrRjWSaJnQg7q/xdYK4TnGaX1SjEq/xdYoEJqUC=t2C=tGDzq/XNq2cX1ZIxq/xdBf6LCzYEYzWHyKRjWSC7t26TB/VdyKRU1AC/1QgEq/xzyKRThAC7tG7L1vqlYeVJqrj7nQcxcKqlYeIkyKRftoa3nQa3qVg7c3qlP3RonQIEnKqlY/wdy/hzY/udY/MJqrDzhSaLcKqlYeVJqrI7WSC7t26TB/YJqrRjWSaJnQg7q/xdYv4TnGaX1SjEq/xdCZEJqUc7toR2nQVTB2JTcS7UcGuTB/VHYT5HyKRjWSC7t26TB/V3yKRU1AC/1QgEq/xzyKRThAC7tG7L1vqlYeqJqrj7nQcxcKqlYeakyKRq1QdS1AIXhSVTB2JTcS7UcGuTB/VECT5oY/hHC/qHyKRjWSC7t26TB/V3yKRU1AC/1QgEq/xzyKRThAC7tG7L1vqlYeqJqrj7nQcxcKqlYeakyKRMnQH7W3FB1AWuMrk0hQ5TB2JTcS7UcGuTB/VzCv5oC/MSY/MJqrDzhSaLcKqlYeVJqrI7WSC7t26TB/YJqrRjWSaJnQg7q/xdYv4TnGaX1SjEq/xdCZEJq7I31QRHhSj7cKFCM3qlP3RonQIEnKqlYeM4y/wdCeh3Cv4ThAC/1QgEq/xdYv4T1GazhSaLcKqlY34ThrDz1QdXtrMTB/VdyKRx1Q72nZ6TB/VE8v4Tara31GDLhvqlP3RonQIEnKqlYehSy/VSCfwSY/MJqrDzhSaLcKqlYeqJqrI7WSC7t26TB/YJqrRjWSaJnQg7q/xdYT4TnGaX1SjEq/xdCAEJq7R=hrkEt3qlP3RonQIEnKqlYeYHy/WSCeh3Cv4ThAC/1QgEq/xdYv4T1GazhSaLcKqlY34ThrDz1QdXtrMTB/VdyKRx1Q72nZ6TB/VE8v4TerkEt3FehQgzq/XNq2cX1ZIxq/xdYzMLCzhHC/qHyKRjWSC7t26TB/VdyKRU1AC/1QgEq/xzyKRThAC7tG7L1vqlYeVJqrj7nQcxcKqlYeIkyKRe1Qc=1vFavvqlP3RonQIEnKqlYe6Hy/qEY/V5CzMJqrDzhSaLcKqlYeqJqrI7WSC7t26TB/6JqrRjWSaJnQg7q/xdYT4TnGaX1SjEq/xdC2EJq7CGqDF3t3qlP3RonQIEnKqlYeYHy/WSCeh3Cv4ThAC/1QgEq/xdYv4T1GazhSaLcKqlY34ThrDz1QdXtrMTB/VdyKRx1Q72nZ6TB/VE8v4TaQRHt2IHq/XNq2cX1ZIxq/xdYzMLCzhHC/qHyKRjWSC7t26TB/VdyKRU1AC/1QgEq/xzyKRThAC7tG7L1vqlYeVJqrj7nQcxcKqlYeIk8v4T1rkLcVI7cGa/cG7=tTqlP3RU1AI7hoI71V1=t2Izq/XtAv4T1rDJtGRjhS0GtSgEW3qlQHEJq2CgWoI7tM1=t2Izq/Xtq2CgWoI7tvHHnvqJqTHjWZFJ1vHzPACE1QETyKRKtG7LnEHjhHCgWoI7tM1=t26TyKRe1Qc=1vFavvqJq7R=hrkEt3qJqUj7tZ17cG7/hvFB1Aa7qT4T6ARXhQ4TyKRBtoI=qDCjt2YTyKRYnQR7WrDEnQkLqDCjt2YTAAEJqrg=nAC7aZ741vqlqrk+qT4T1QgEWrk4PvqlY35gYfh5BewHBeMSYfuHYeuo8v4T1rkLcDF31Q17WraLhSazq/XNqrI71rDHtZ6TB/VdCeqLYfh3Cv4ThAF4tGMTB/VdBe6LCv4TWSa3nQhTB/VdBe6LCv4TWSDLW3qlYeVHYT54C/qHyKR0tSg=q/xdYeUSy/YoCv4TtQ7Lq/xdY/hSy/h3Cv4TWo7zcGa0q/xdYeu3y/uoCAEJq2C/Wra7t7R7WSkJcAIXtS5TB2JTcS7UcGuTB/VgY/wJqrj7nQcxcKqlYew5YK4ThA1jnQdAnQIEnKqlYeU3YK4ThA1jnQdq1Q72nZ6TB/V4YzqJq2FXPGaJMrDEnQ9TB/VJqrI7cr7/1aFXPGaJMrDEnQ9TB/VJqrd=1S7/hQd6nAj7tDRjcG7=q/xd8v4TWSC31QaLI2RjtQMTB2JT12RjtQaAnQIEnKqlYeU3YK4T12RjtQaq1Q72nZ6TB/V4BfwJqr13hQH7IGa4cGuTB/qEyKRrWrD01MC=tGk3IGa4cGuTB/qEyKRrWrD01aFXPGaJIGa4cGuTB/qEyKR=Wr77t2IjcG7=tTqlqrdjtrIzhSD41vH4Wr70hARgqT4TtoRX1QgEhAIXtSgFtrcJ1vqlYZEJqrC=tGk3IGa4cGuTB2JThSkJtoRV1AFEnKqlY/6Jq2FXPGaJIGa4cGuTB/qEyKRTnAIzMGa36Sjjtrg7tKqlBK4TcGkEhQdKnAIzq/x3CK4TnGDz6Qd4nGVTB2I3cQMJqrjjWHI3hQgzWGD31Qg/PvqlcZRH1AEJqrC=tGk3ISD0cA6TB2JThSkJtoRZhQHHcKqlq2R7hzq4Y/wTyKRzcAF4toRE1QIftSd=W7C4hQC7W3qlQ3Rj1GkT1vH31SqTyKRUnAC4tGDgyAwzqT4TWfYTyKR31QY3Yfq4qT4TWoR2hTRcyKR4YHCHWZF=W26TB2I3cQMJq2R7hzq4Y/FecAF4toREq/XEW2a7yKRzWrcTMoa4WGk3cKqlcZRH1v4T1G7zWGdjPawzMoa4WGk3cKqlcZRH1AHk",
        };

        private readonly TransactionRequest transactionMockData = new TransactionRequest()
        {
            Account = "testAccount",
            Di = "testDi",
            __di = "test__di",
        };

        public LinkingServiceTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockAppSettings = new Mock<IAppSettings>();
            _mockRedisService = new Mock<IRedisService>();
            _mockRedisDatabase = new Mock<IRedisDatabase>();
            _mockDatabase = new Mock<IDatabase>();
            _mockBatch = new Mock<IBatch>();
            _mockMassTransitProducer = new Mock<IMassTransitProducer>();

            // Setup the Redis service to return the mock database
            _mockRedisService.Setup(r => r.RedisDatabase).Returns(_mockRedisDatabase.Object);
            _mockRedisDatabase.Setup(d => d.Database).Returns(_mockDatabase.Object);
            _mockDatabase.Setup(d => d.CreateBatch(It.IsAny<object>())).Returns(_mockBatch.Object);

            _linkingService = new LinkingService(_mockLogger.Object, _mockAppSettings.Object, _mockRedisService.Object, _mockMassTransitProducer.Object);
        }

        [Fact]
        public async Task LinkingOnInsert_WhenHasThreeDataTypes_ShouldPublishMessageToQueue()
        {
            // Arrange
            var fp4Json = JsonSerializer.Serialize(fp4MockData);
            var fp5Json = JsonSerializer.Serialize(fp5MockData);
            var transactionJson = JsonSerializer.Serialize(transactionMockData);

            // Mock batch operations
            var fp4Task = Task.FromResult<RedisValue>(fp4Json);
            var fp5Task = Task.FromResult<RedisValue>(fp5Json);
            var transactionTask = Task.FromResult<RedisValue>(transactionJson);

            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP4_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp4Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP5_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp5Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.TRANSACTION_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(transactionTask);

            // Mock the HashDeleteAsync for cleanup
            _mockRedisService.Setup(r => r.HashDeleteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(3);

            // Act
            var result = await _linkingService.LinkingOnInsert("testSession");

            // Assert
            result.Should().BeTrue();
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Once);
            _mockBatch.Verify(b => b.Execute(), Times.Once);
            _mockRedisService.Verify(r => r.HashDeleteAsync("testSession",
                It.Is<string[]>(keys => keys.Contains(_linkingService.FP4_REDIS_KEY) &&
                                      keys.Contains(_linkingService.FP5_REDIS_KEY) &&
                                      keys.Contains(_linkingService.TRANSACTION_REDIS_KEY))), Times.Once);
        }

        [Fact]
        public async Task LinkingOnInsert_WhenMissingFP4_ShouldSkip()
        {
            // Arrange
            var fp5Json = JsonSerializer.Serialize(fp5MockData);
            var transactionJson = JsonSerializer.Serialize(transactionMockData);

            // Mock batch operations with missing FP4
            var fp4Task = Task.FromResult<RedisValue>(RedisValue.Null);
            var fp5Task = Task.FromResult<RedisValue>(fp5Json);
            var transactionTask = Task.FromResult<RedisValue>(transactionJson);

            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP4_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp4Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP5_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp5Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.TRANSACTION_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(transactionTask);

            // Act
            var result = await _linkingService.LinkingOnInsert("testSession");

            // Assert
            result.Should().BeFalse();
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
            _mockBatch.Verify(b => b.Execute(), Times.Once);
        }

        [Fact]
        public async Task LinkingOnInsert_WhenMissingFP5_ShouldSkip()
        {
            // Arrange
            var fp4Json = JsonSerializer.Serialize(fp4MockData);
            var transactionJson = JsonSerializer.Serialize(transactionMockData);

            // Mock batch operations with missing FP5
            var fp4Task = Task.FromResult<RedisValue>(fp4Json);
            var fp5Task = Task.FromResult<RedisValue>(RedisValue.Null);
            var transactionTask = Task.FromResult<RedisValue>(transactionJson);

            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP4_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp4Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP5_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp5Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.TRANSACTION_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(transactionTask);

            // Act
            var result = await _linkingService.LinkingOnInsert("testSession");

            // Assert
            result.Should().BeFalse();
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
            _mockBatch.Verify(b => b.Execute(), Times.Once);
        }

        [Fact]
        public async Task LinkingOnInsert_WhenMissingTransaction_ShouldSkip()
        {
            // Arrange
            var fp4Json = JsonSerializer.Serialize(fp4MockData);
            var fp5Json = JsonSerializer.Serialize(fp5MockData);

            // Mock batch operations with missing Transaction
            var fp4Task = Task.FromResult<RedisValue>(fp4Json);
            var fp5Task = Task.FromResult<RedisValue>(fp5Json);
            var transactionTask = Task.FromResult<RedisValue>(RedisValue.Null);

            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP4_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp4Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP5_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp5Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.TRANSACTION_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(transactionTask);

            // Act
            var result = await _linkingService.LinkingOnInsert("testSession");

            // Assert
            result.Should().BeFalse();
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
            _mockBatch.Verify(b => b.Execute(), Times.Once);
        }

        [Fact]
        public async Task LinkingOnInsert_WhenExceptionOccurs_ShouldLogErrorAndRethrow()
        {
            // Arrange
            _mockDatabase.Setup(d => d.CreateBatch(It.IsAny<object>()))
                .Throws(new RedisConnectionException(ConnectionFailureType.SocketClosed, "Connection lost"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RedisConnectionException>(() =>
                _linkingService.LinkingOnInsert("testSession"));

            _mockLogger.Verify(
                l => l.ErrorAsync(It.IsAny<string>(), It.IsAny<Exception>()),
                Times.Once);
        }

        [Fact]
        public void LinkFingerprintsAttributes_ShouldReturnCorrectModel()
        {
            // Arrange
            var expected = new APFingerprintModel
            {
                Audio = Base64.Decode(fp4MockData.Audio, BASE64_KEY),
                Browser = Base64.Decode(fp4MockData.Browser, BASE64_KEY),
                Graphics = Base64.Decode(fp5MockData.Graphics, BASE64_KEY),
                Hardware = Base64.Decode(fp4MockData.Hardware, BASE64_KEY),
                Preferences = Base64.Decode(fp4MockData.Preferences, BASE64_KEY)
            };

            // Act
            var actual = _linkingService.LinkFingerprintsAttributes([fp4MockData, fp5MockData]);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void LinkFingerprintsAttributes_WhenMissingFP5_ShouldReturnCorrectModel()
        {
            // Arrange
            var expected = new APFingerprintModel
            {
                Audio = Base64.Decode(fp4MockData.Audio, BASE64_KEY),
                Browser = Base64.Decode(fp4MockData.Browser, BASE64_KEY),
                Hardware = Base64.Decode(fp4MockData.Hardware, BASE64_KEY),
                Preferences = Base64.Decode(fp4MockData.Preferences, BASE64_KEY)
            };

            // Act
            var actual = _linkingService.LinkFingerprintsAttributes([fp4MockData]);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void LinkFingerprintsAttributes_WhenMissingFP4_ShouldReturnCorrectModel()
        {
            // Arrange
            var expected = new APFingerprintModel
            {
                Graphics = Base64.Decode(fp5MockData.Graphics, BASE64_KEY),
            };

            // Act
            var actual = _linkingService.LinkFingerprintsAttributes([fp5MockData]);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void BuildDeviceModel_ShouldReturnCorrectModel()
        {
            // Arrange

            // Act
            var actual = _linkingService.BuildDeviceModel(transactionMockData);

            // Assert
            actual.Should().NotBeNull();
            Assert.Equal(transactionMockData.Di, actual.Di);
        }

        [Fact]
        public void BuildMessageQueue_ShouldReturnCorrectModel()
        {
            // Arrange
            var expected = new APFingerprintModel
            {
                Audio = Base64.Decode(fp4MockData.Audio, BASE64_KEY),
                Browser = Base64.Decode(fp4MockData.Browser, BASE64_KEY),
                Graphics = Base64.Decode(fp5MockData.Graphics, BASE64_KEY),
                Hardware = Base64.Decode(fp4MockData.Hardware, BASE64_KEY),
                Preferences = Base64.Decode(fp4MockData.Preferences, BASE64_KEY)
            };

            // Act
            var actual = _linkingService.BuildMessageQueue(transactionMockData, fp4MockData, fp5MockData);

            // Assert
            actual.Should().NotBeNull();
            actual.Fingerprint.Should().BeEquivalentTo(expected);
            actual.Account.Should().BeEquivalentTo(transactionMockData.Account);
            actual.Device.Di.Should().BeEquivalentTo(transactionMockData.Di);
        }

        [Fact]
        public async Task LinkingOnExpired_WhenTransactionExists_ShouldPublishMessageToQueue()
        {
            // Arrange
            var fp4Json = JsonSerializer.Serialize(fp4MockData);
            var fp5Json = JsonSerializer.Serialize(fp5MockData);
            var transactionJson = JsonSerializer.Serialize(transactionMockData);

            // Mock batch operations
            var fp4Task = Task.FromResult<RedisValue>(fp4Json);
            var fp5Task = Task.FromResult<RedisValue>(fp5Json);
            var transactionTask = Task.FromResult<RedisValue>(transactionJson);

            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP4_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp4Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP5_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp5Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.TRANSACTION_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(transactionTask);

            // Mock the HashDeleteAsync for cleanup
            _mockRedisService.Setup(r => r.HashDeleteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(3);

            // Act
            var result = await _linkingService.LinkingOnExpired("testSession_shadow");

            // Assert
            result.Should().BeTrue();
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Once);
            _mockBatch.Verify(b => b.Execute(), Times.Once);
            _mockRedisService.Verify(r => r.HashDeleteAsync("testSession",
                It.Is<string[]>(keys => keys.Contains(_linkingService.FP4_REDIS_KEY) &&
                                      keys.Contains(_linkingService.FP5_REDIS_KEY) &&
                                      keys.Contains(_linkingService.TRANSACTION_REDIS_KEY))), Times.Once);
        }

        [Fact]
        public async Task LinkingOnExpired_WhenTransactionMissing_ShouldDeleteAndReturnFalse()
        {
            // Arrange
            var fp4Json = JsonSerializer.Serialize(fp4MockData);
            var fp5Json = JsonSerializer.Serialize(fp5MockData);

            // Mock batch operations with missing Transaction
            var fp4Task = Task.FromResult<RedisValue>(fp4Json);
            var fp5Task = Task.FromResult<RedisValue>(fp5Json);
            var transactionTask = Task.FromResult<RedisValue>(RedisValue.Null);

            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP4_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp4Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP5_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp5Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.TRANSACTION_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(transactionTask);

            // Mock the HashDeleteAsync for cleanup
            _mockRedisService.Setup(r => r.HashDeleteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(3);

            // Act
            var result = await _linkingService.LinkingOnExpired("testSession_shadow");

            // Assert
            result.Should().BeFalse();
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
            _mockBatch.Verify(b => b.Execute(), Times.Once);
            _mockRedisService.Verify(r => r.HashDeleteAsync("testSession",
                It.Is<string[]>(keys => keys.Contains(_linkingService.FP4_REDIS_KEY) &&
                                      keys.Contains(_linkingService.FP5_REDIS_KEY) &&
                                      keys.Contains(_linkingService.TRANSACTION_REDIS_KEY))), Times.Once);
        }

        [Fact]
        public async Task LinkingOnExpired_WhenMissingFingerprints_ShouldStillPublishMessage()
        {
            // Arrange
            var transactionJson = JsonSerializer.Serialize(transactionMockData);

            // Mock batch operations with missing fingerprints but transaction exists
            var fp4Task = Task.FromResult<RedisValue>(RedisValue.Null);
            var fp5Task = Task.FromResult<RedisValue>(RedisValue.Null);
            var transactionTask = Task.FromResult<RedisValue>(transactionJson);

            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP4_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp4Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP5_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp5Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.TRANSACTION_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(transactionTask);

            // Mock the HashDeleteAsync for cleanup
            _mockRedisService.Setup(r => r.HashDeleteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(3);

            // Act
            var result = await _linkingService.LinkingOnExpired("testSession_shadow");

            // Assert
            result.Should().BeTrue();
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Once);
            _mockBatch.Verify(b => b.Execute(), Times.Once);
        }
    }
}
