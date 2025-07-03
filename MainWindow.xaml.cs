using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PumpFunSniper
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<TokenInfo> _tokens = new();
        private ClientWebSocket _wsClient;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();
            TokenGrid.ItemsSource = _tokens;

            // Запуск мониторинга
            StartMonitoring();
        }

        private async void StartMonitoring()
        {
            _cts = new CancellationTokenSource();
            _wsClient = new ClientWebSocket();
            string wsUrl = "wss://mainnet.helius-rpc.com/?api-key=8051d855-723f-4a71-92ed-23d7e7136502";

            try
            {
                await _wsClient.ConnectAsync(new Uri(wsUrl), _cts.Token);
                Console.WriteLine("Подключение установлено");

                // Отправка запроса на подписку
                var subscription = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "logsSubscribe",
                    @params = new[]
                    {
                        new
                        {
                            mentions = new[] { "6EF8rrecthR5Dkzon8Nwu78hRvfCKubJ14M5uBEwF6P" },
                            commitment = "finalized"
                        }
                    }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(subscription);
                var buffer = Encoding.UTF8.GetBytes(json);
                await _wsClient.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);

                // Чтение ответов
                await ReceiveMessages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[1024];
            while (_wsClient.State == WebSocketState.Open)
            {
                var result = await _wsClient.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Получено: {message}");

                    // Простая проверка на создание токена (нужна доработка парсинга)
                    if (message.Contains("Instruction: Create") || message.ToLower().Contains("create"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _tokens.Add(new TokenInfo
                            {
                                TokenAddress = "Неизвестно",
                                Developer = "Неизвестно",
                                MarketCap = 0
                            });
                        });
                        Console.WriteLine("Новый токен обнаружен");
                    }
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _cts?.Cancel();
            _wsClient?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закрытие окна", CancellationToken.None);
            base.OnClosed(e);
        }
    }

    public class TokenInfo : INotifyPropertyChanged
    {
        private string _tokenAddress;
        private string _developer;
        private double _marketCap;

        public string TokenAddress
        {
            get => _tokenAddress;
            set { _tokenAddress = value; OnPropertyChanged(nameof(TokenAddress)); }
        }

        public string Developer
        {
            get => _developer;
            set { _developer = value; OnPropertyChanged(nameof(Developer)); }
        }

        public double MarketCap
        {
            get => _marketCap;
            set { _marketCap = value; OnPropertyChanged(nameof(MarketCap)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}