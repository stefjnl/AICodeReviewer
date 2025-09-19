
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AICodeReviewer.Web.Infrastructure.Services;
using System.Threading.Tasks;
using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Http;
using System.Threading;


using AICodeReviewer.Web.Infrastructure.Extensions;

using System.Text;

namespace AICodeReviewer.Web.Tests
{
    

    

    public class SignalRBroadcastServiceTests
    {
        private readonly Mock<ILogger<SignalRBroadcastService>> _mockLogger;
        private readonly Mock<IHubContext<ProgressHub>> _mockHubContext;
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly TestSession _testSession;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<IGroupManager> _mockGroupManager;
        private readonly SignalRBroadcastService _signalRBroadcastService;

        public SignalRBroadcastServiceTests()
        {
            _mockLogger = new Mock<ILogger<SignalRBroadcastService>>();
            _mockHubContext = new Mock<IHubContext<ProgressHub>>();
            _mockMemoryCache = new Mock<IMemoryCache>();
            _testSession = new TestSession();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockGroupManager = new Mock<IGroupManager>();

            _mockHubContext.Setup(h => h.Clients.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockHubContext.Setup(h => h.Groups).Returns(_mockGroupManager.Object);

            _signalRBroadcastService = new SignalRBroadcastService(_mockHubContext.Object, _mockLogger.Object, _mockMemoryCache.Object);
        }

        [Fact]
        public async Task BroadcastProgressAsync_SendsProgressUpdate()
        {
            // Arrange
            var analysisId = "123";
            var status = "In progress";
            var cacheEntry = new Mock<ICacheEntry>();
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(cacheEntry.Object);

            // Act
            await _signalRBroadcastService.BroadcastProgressAsync(analysisId, status);

            // Assert
            _mockClientProxy.Verify(c => c.SendCoreAsync("UpdateProgress", It.Is<object[]>(o => ((ProgressDto)o[0]).Status == status), default(CancellationToken)), Times.Once);
            _mockMemoryCache.Verify(m => m.CreateEntry($"analysis_{analysisId}"), Times.Once);
        }

        [Fact]
        public async Task BroadcastCompleteAsync_SendsCompleteUpdateAndStoresInCacheAndSession()
        {
            // Arrange
            var analysisId = "123";
            var result = "Analysis result";
            var cacheEntry = new Mock<ICacheEntry>();
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(cacheEntry.Object);

            // Act
            await _signalRBroadcastService.BroadcastCompleteAsync(analysisId, result, _testSession);

            // Assert
            _mockClientProxy.Verify(c => c.SendCoreAsync("UpdateProgress", It.Is<object[]>(o => ((ProgressDto)o[0]).Result == result), default(CancellationToken)), Times.Once);
            _mockMemoryCache.Verify(m => m.CreateEntry($"analysis_{analysisId}"), Times.Once);
            Assert.Equal(analysisId, _testSession.GetString("AnalysisId"));
        }

        [Fact]
        public async Task BroadcastErrorAsync_SendsErrorUpdateAndStoresInCache()
        {
            // Arrange
            var analysisId = "123";
            var error = "Analysis error";
            var cacheEntry = new Mock<ICacheEntry>();
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(cacheEntry.Object);

            // Act
            await _signalRBroadcastService.BroadcastErrorAsync(analysisId, error);

            // Assert
            _mockClientProxy.Verify(c => c.SendCoreAsync("UpdateProgress", It.Is<object[]>(o => ((ProgressDto)o[0]).Error == error), default(CancellationToken)), Times.Once);
            _mockMemoryCache.Verify(m => m.CreateEntry($"analysis_{analysisId}"), Times.Once);
        }
    }
}
