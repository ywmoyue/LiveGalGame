using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage.Streams;
using Composition.WindowsRuntimeHelpers;

namespace LiveGalGameWAS
{
    public class ScreenCaptureService
    {
        private GraphicsCaptureItem? _captureItem;
         private Direct3D11CaptureFramePool? _framePool;
         private GraphicsCaptureSession? _session;
         private IDirect3DDevice? _device;
         private WriteableBitmap? _currentFrame;
         private bool _isCapturing = false;

        public event Action<WriteableBitmap>? FrameCaptured;

        public async Task<bool> InitializeAsync()
        {
            try
            {
                // 检查屏幕捕获权限
                if (!GraphicsCaptureSession.IsSupported())
                {
                    System.Diagnostics.Debug.WriteLine("屏幕捕获不支持");
                    return false;
                }

                // 获取默认设备
                _device = Direct3D11Helper.CreateDevice();
                await Task.CompletedTask; // 避免异步警告
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"屏幕捕获初始化失败: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetAvailableWindowsAsync()
        {
            var windows = new List<string>();
            
            try
            {
                throw new NotImplementedException();

                //// 获取所有可捕获的窗口
                //var items = GraphicsCaptureSession.IsSupported() ? await GraphicsCaptureItem.FindAllAsync(GraphicsCaptureItemKind.Programmatic) : new List<GraphicsCaptureItem>();

                //foreach (var item in items)
                //{
                //    if (!string.IsNullOrEmpty(item.DisplayName))
                //    {
                //        windows.Add(item.DisplayName);
                //    }
                //}

                //return windows;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取窗口列表失败: {ex.Message}");
                return windows;
            }
        }

        public async Task<bool> StartCaptureAsync(string? windowTitle = null)
        {
            if (_isCapturing)
                return false;

            try
            {
                GraphicsCaptureItem? item = null;
                
                if (string.IsNullOrEmpty(windowTitle))
        {
            // 捕获整个屏幕
            item = await GetPrimaryDisplayAsync();
        }
        else
        {
            // 捕获指定窗口
            item = await GetWindowByTitleAsync(windowTitle);
        }

                if (item == null)
                {
                    System.Diagnostics.Debug.WriteLine("未找到可捕获的项目");
                    return false;
                }

                _captureItem = item;
                
                // 创建帧池
                var framePool = Direct3D11CaptureFramePool.Create(
                    _device,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    2,
                    _captureItem.Size);

                _session = framePool.CreateCaptureSession(_captureItem);
                _session.IsCursorCaptureEnabled = false; // 不捕获光标
                
                framePool.FrameArrived += OnFrameArrived;
                
                _session.StartCapture();
                _isCapturing = true;
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"开始屏幕捕获失败: {ex.Message}");
                return false;
            }
        }

        private async Task<GraphicsCaptureItem?> GetPrimaryDisplayAsync()
        {
            try
            {
                throw new NotImplementedException();
                //var items = GraphicsCaptureSession.IsSupported() ? await GraphicsCaptureItem.FindAllAsync(GraphicsCaptureItemKind.Programmatic) : new List<GraphicsCaptureItem>();
                // return items.FirstOrDefault(item => item.DisplayName?.Contains("Display") == true);
            }
            catch
            {
                return null;
            }
        }

        private async Task<GraphicsCaptureItem?> GetWindowByTitleAsync(string title)
        {
            try
            {
                throw new NotImplementedException();
                //var items = GraphicsCaptureSession.IsSupported() ? await GraphicsCaptureItem.FindAllAsync(GraphicsCaptureItemKind.Programmatic) : new List<GraphicsCaptureItem>();
                // return items.FirstOrDefault(item => 
                //     item.DisplayName?.Contains(title, StringComparison.OrdinalIgnoreCase) == true);
            }
            catch
            {
                return null;
            }
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            using (var frame = sender.TryGetNextFrame())
            {
                if (frame != null)
                {
                    // 处理捕获的帧
                    ProcessFrame(frame);
                }
            }
        }

        private async void ProcessFrame(Direct3D11CaptureFrame frame)
        {
            try
            {
                var bitmap = new WriteableBitmap(frame.ContentSize.Width, frame.ContentSize.Height);
                
                // 这里需要将Direct3D表面转换为WriteableBitmap
                // 简化处理：创建一个占位符位图
                using (var stream = new InMemoryRandomAccessStream())
                {
                    // 实际实现需要更复杂的D3D到Bitmap转换
                    // 这里使用简化版本
                    _currentFrame = bitmap;
                    FrameCaptured?.Invoke(bitmap);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理帧失败: {ex.Message}");
            }
        }

        public async Task StopCaptureAsync()
        {
            if (!_isCapturing)
                return;

            try
            {
                _session?.Dispose();
                _framePool?.Dispose();
                _isCapturing = false;
                await Task.CompletedTask; // 避免异步警告
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"停止捕获失败: {ex.Message}");
            }
        }

        public async Task DisposeAsync()
        {
            await StopCaptureAsync();
            _device?.Dispose();
            _device = null;
            await Task.CompletedTask; // 避免异步警告
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public WriteableBitmap GetCurrentFrame() => _currentFrame;
        public bool IsCapturing => _isCapturing;
    }
}