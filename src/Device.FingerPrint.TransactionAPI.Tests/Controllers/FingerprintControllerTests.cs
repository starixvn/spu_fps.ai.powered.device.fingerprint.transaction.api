using Device.FingerPrint.TransactionAPI.Controllers;
using Device.FingerPrint.TransactionAPI.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Controllers
{
    public class FingerprintControllerTests
    {
        private readonly Mock<ILogger> mockLogger;
        private readonly Mock<IFingerprintService> mockFingerprintService;
        private readonly FingerprintController controller;

        public FingerprintControllerTests()
        {
            mockLogger = new Mock<ILogger>();
            mockFingerprintService = new Mock<IFingerprintService>();
            controller = new FingerprintController(mockLogger.Object, mockFingerprintService.Object);
        }

        [Fact]
        public async Task InsertFP4_ShouldReturnOk_WhenRequestIsValid()
        {
            // Arrange
            var fingerprintRequest = "P3Rzn3qlqrDUCQMdCrhd1ehgCe6S1e65hSMgYeYSYSVSBfa/1/UdqT4T12wTB2JThAMTBTRNAKRjcQIXtH4TB20WqrDH1G7=AKqlY/W3y/VzCzYECeh4CzV4BfWJAKRzcGDEcACWq/XWqrCJ1QDLAKRkyD4ThAaUnQkKhAC7eGDE1Qg/Pa4TB/V4YT5g8vqJqrR3q/xTPH4Th2R=coC7WUgjtQaWq/XWqUCxWrk01a4TyD4TtoCBhQH7AKqlAKRAnQgUtoczAKqJAKRHWSa36Qc7t2IWq/XWqUH=Pr7JtGV=Cv54qKjAnQgUtoczqVgMqfV4y/wNqDcXt/hEB3F5C/6XqVD4WGd7aSaTvS7EyzMzC35zCTwxvEjMeM4JqGdXnSMuISa/nS9XqVCxWrk01v9dYzULYK54y/wuMSDrhARXyzMzC35zC74TyD4TcraL1Gk3AKqlAKRZtSk2tGMuvQg/y74TyD4TcraL1Gk3Irdjcrk3WH4TB70WqrCxWrk01a4TAvdWq2FJcQcXt2CWq/XtPH4TtrD01a4TB74TMVIGqD1X1Ac7W74TyD4T1GazhoRXWZIXtSgWq/XWq7F=W2Ijhrd7qVI=hoa01QgEqV1=WrHjcD4TyD4T1r7J1QgjtQaWq/XWqr7LcGa3trDJyAFU1THSnQao1ARWq2EJPH4TtrD01a4TB74T6Sj3tSH7qDFVITFQnQao1ARWqTdWqrI7WSC3nAFEnQkLAKqlAKR6toREhQRJ1vFVtSCHtQaLcKFGtoR0hAIWqTdWqr1XtGaLhQH7AKqlAKRXt2I7WrgjtKH41Gh0cr77cSa3AKRkyZ0WqrgjtQaWq/XWqUCxWrk0nAa0qDFVITFQnQao1ARWqTdWqrI7WSC3nAFEnQkLAKqlAKR6toREhQRJ1vFVtSCHtQaLcKFGtoR0hAIWqTdWqr1XtGaLhQH7AKqlAKRXt2I7WrgjtKH41Gh0cr77cSa3AKRkyZ0WqrgjtQaWq/XWqUHXhoR=WSkrcKFD1Gc7qDFVITFQnQao1ARWqTdWqrI7WSC3nAFEnQkLAKqlAKR6toREhQRJ1vFVtSCHtQaLcKFGtoR0hAIWqTdWqr1XtGaLhQH7AKqlAKRXt2I7WrgjtKH41Gh0cr77cSa3AKRkyZ0WqrgjtQaWq/XWq7c7hU0XcKFTcQ7JcKHXtTF6IV1WqTdWqrI7WSC3nAFEnQkLAKqlAKR6toREhQRJ1vFVtSCHtQaLcKFGtoR0hAIWqTdWqr1XtGaLhQH7AKqlAKRXt2I7WrgjtKH41Gh0cr77cSa3AKRkAvdWq2FU171X1Ac7WUaLhQRJ1QIWq/XEW2a78vqJqrjoq/xTPH4TWGdjcG1=WrHWq/XWq7cXtrI=coCWqTdWqrC4cMCJhACzAKqlAKRWqTdWqrD3hSjXcGa/cZa31a4TB74TPfuSyou5C79SCD4TyD4TtoC/WZaWq/XWq7cXtrI=coYNqZu5C74TyD4TnGD31ZcjWraftSg/cAR31Qg/Pa4TB/q4yD4T1GaSnQC7eQa0toRgAKqlBZETyKR41Tqlq20WqrdjtrcHhQc7WH4TB70WqraLyaaeAKqJAKR7t74TAvdWq2IXtQaltSg7AKqlAKRFWS7jyERjtrc+tS0WqTdWq2I=cQCxMoa4WGk3cD4TBr1jtZC7yD4TWSazWS7=t7CEtoRj1SaWq/XEW2a7yD4TtoF7tUIjcGDThAC7AKql1rDJWSMJAKRJtSCjtDCEtoRj1SaWq/XEW2a7yD4TnQgU1Aj71VIKAKqlcZRH1vdWqrC=tS0X1ACDtrDTtGaUAKqlcZRH1AET8v4TnZYTBTRThQVHBf77CfIU1G6zYzDT1fVgBfY51f1jBeq4h/Mg1TRk";

            // Act
            var result = await controller.InsertFP4(fingerprintRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task InsertFP4_ShouldReturnBadRequest_WhenRequestIsInvalid()
        {
            // Arrange
            var fingerprintRequest = string.Empty;

            // Act
            var result = await controller.InsertFP4(fingerprintRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task InsertFP5_ShouldReturnOk_WhenRequestIsValid()
        {
            // Arrange
            var fingerprintRequest = "P3Rzn3qlqrDUCQMdCrhd1ehgCe6S1e65hSMgYeYSYSVSBfa/1/UdqT4T12wTB2JThAMTBTRNAKRjcQIXtH4TB20WqrDH1G7=AKqlY/W3y/VzCzYECeh4CzV4BfWJAKRzcGDEcACWq/XWqrCJ1QDLAKRkyD4ThAaUnQkKhAC7eGDE1Qg/Pa4TB/V4YT5g8vqJqrR3q/xTPH4Th2R=coC7WUgjtQaWq/XWqUCxWrk01a4TyD4TtoCBhQH7AKqlAKRAnQgUtoczAKqJAKRHWSa36Qc7t2IWq/XWqUH=Pr7JtGV=Cv54qKjAnQgUtoczqVgMqfV4y/wNqDcXt/hEB3F5C/6XqVD4WGd7aSaTvS7EyzMzC35zCTwxvEjMeM4JqGdXnSMuISa/nS9XqVCxWrk01v9dYzULYK54y/wuMSDrhARXyzMzC35zC74TyD4TcraL1Gk3AKqlAKRZtSk2tGMuvQg/y74TyD4TcraL1Gk3Irdjcrk3WH4TB70WqrCxWrk01a4TAvdWq2FJcQcXt2CWq/XtPH4TtrD01a4TB74TMVIGqD1X1Ac7W74TyD4T1GazhoRXWZIXtSgWq/XWq7F=W2Ijhrd7qVI=hoa01QgEqV1=WrHjcD4TyD4T1r7J1QgjtQaWq/XWqr7LcGa3trDJyAFU1THSnQao1ARWq2EJPH4TtrD01a4TB74T6Sj3tSH7qDFVITFQnQao1ARWqTdWqrI7WSC3nAFEnQkLAKqlAKR6toREhQRJ1vFVtSCHtQaLcKFGtoR0hAIWqTdWqr1XtGaLhQH7AKqlAKRXt2I7WrgjtKH41Gh0cr77cSa3AKRkyZ0WqrgjtQaWq/XWqUCxWrk0nAa0qDFVITFQnQao1ARWqTdWqrI7WSC3nAFEnQkLAKqlAKR6toREhQRJ1vFVtSCHtQaLcKFGtoR0hAIWqTdWqr1XtGaLhQH7AKqlAKRXt2I7WrgjtKH41Gh0cr77cSa3AKRkyZ0WqrgjtQaWq/XWqUHXhoR=WSkrcKFD1Gc7qDFVITFQnQao1ARWqTdWqrI7WSC3nAFEnQkLAKqlAKR6toREhQRJ1vFVtSCHtQaLcKFGtoR0hAIWqTdWqr1XtGaLhQH7AKqlAKRXt2I7WrgjtKH41Gh0cr77cSa3AKRkyZ0WqrgjtQaWq/XWq7c7hU0XcKFTcQ7JcKHXtTF6IV1WqTdWqrI7WSC3nAFEnQkLAKqlAKR6toREhQRJ1vFVtSCHtQaLcKFGtoR0hAIWqTdWqr1XtGaLhQH7AKqlAKRXt2I7WrgjtKH41Gh0cr77cSa3AKRkAvdWq2FU171X1Ac7WUaLhQRJ1QIWq/XEW2a78vqJqrjoq/xTPH4TWGdjcG1=WrHWq/XWq7cXtrI=coCWqTdWqrC4cMCJhACzAKqlAKRWqTdWqrD3hSjXcGa/cZa31a4TB74TPfuSyou5C79SCD4TyD4TtoC/WZaWq/XWq7cXtrI=coYNqZu5C74TyD4TnGD31ZcjWraftSg/cAR31Qg/Pa4TB/q4yD4T1GaSnQC7eQa0toRgAKqlBZETyKR41Tqlq20WqrdjtrcHhQc7WH4TB70WqraLyaaeAKqJAKR7t74TAvdWq2IXtQaltSg7AKqlAKRFWS7jyERjtrc+tS0WqTdWq2I=cQCxMoa4WGk3cD4TBr1jtZC7yD4TWSazWS7=t7CEtoRj1SaWq/XEW2a7yD4TtoF7tUIjcGDThAC7AKql1rDJWSMJAKRJtSCjtDCEtoRj1SaWq/XEW2a7yD4TnQgU1Aj71VIKAKqlcZRH1vdWqrC=tS0X1ACDtrDTtGaUAKqlcZRH1AET8v4TnZYTBTRThQVHBf77CfIU1G6zYzDT1fVgBfY51f1jBeq4h/Mg1TRk";

            // Act
            var result = await controller.InsertFP5(fingerprintRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task InsertFP5_ShouldReturnBadRequest_WhenRequestIsInvalid()
        {
            // Arrange
            var fingerprintRequest = string.Empty;

            // Act
            var result = await controller.InsertFP5(fingerprintRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
