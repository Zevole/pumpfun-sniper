using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PumpFunSniper
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ObservableCollection<TokenInfo> _tokens = new();
        private ClientWebSocket? _wsClient;
        private CancellationTokenSource? _cts;
        private string _status = "Статус: Ожидание подключения...";

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<TokenInfo> Tokens => _tokens;
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Console.WriteLine("[DEBUG] Конструктор инициализирован");
            Loaded += async (s, e) =>
            {
                Console.WriteLine("[DEBUG] Событие Loaded вызвано");
                await StartMonitoring();
            };
        }

        private async Task StartMonitoring()
        {
            Console.WriteLine("[DEBUG] Начало StartMonitoring");
            Status = "Статус: Инициализация подключения...";
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(300)); // Увеличен таймаут до 300 секунд
            _wsClient = new ClientWebSocket();
            string wsUrl = "wss://mainnet.helius-rpc.com/?api-key=8051d855-723f-4a71-92ed-23d7e7136502"; // Замените на ваш актуальный Helius URL
            string apiKey = "8051d855-723f-4a71-92ed-23d7e7136502"; // Замените на ваш API-ключ от Helius

            try
            {
                Console.WriteLine("[DEBUG] Установка заголовка Authorization");
                _wsClient.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                Console.WriteLine("[DEBUG] Попытка подключения к: {wsUrl}");
                await _wsClient.ConnectAsync(new Uri(wsUrl), _cts.Token);
                Console.WriteLine("[DEBUG] Подключение установлено");

                Status = "Статус: Подключение установлено";
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

                var json = JsonSerializer.Serialize(subscription);
                Console.WriteLine("[DEBUG] Отправка запроса подписки: {json}");
                var buffer = Encoding.UTF8.GetBytes(json);
                await _wsClient.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
                Console.WriteLine("[DEBUG] Запрос подписки отправлен");

                await ReceiveMessages();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Ошибка: {ex.Message}");
                Status = $"Статус: Ошибка - {ex.Message}";
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ReceiveMessages()
        {
            if (_wsClient == null) return;
            var buffer = new byte[1024 * 4];
            while (_wsClient.State == WebSocketState.Open)
            {
                var result = await _wsClient.ReceiveAsync(new ArraySegment<byte>(buffer), _cts?.Token ?? CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine("[DEBUG] Получено: {message}");

                    try
                    {
                        var data = JsonSerializer.Deserialize<JsonResponse>(message);
                        if (data?.Result?.Value?.Logs != null)
                        {
                            var logs = data.Result.Value.Logs;
                            Console.WriteLine("Логи: " + string.Join(", ", logs));
                            if (logs.Any(l => l.Contains("Instruction: Create") || l.ToLower().Contains("create")))
                            {
                                string tokenAddress = ExtractTokenAddress(logs) ?? "Неизвестно";
                                string developer = "Неизвестно";
                                double marketCap = 0;

                                Dispatcher.Invoke(() =>
                                {
                                    _tokens.Add(new TokenInfo
                                    {
                                        TokenAddress = tokenAddress,
                                        Developer = developer,
                                        MarketCap = marketCap
                                    });
                                });
                                Console.WriteLine($"Новый токен обнаружен: Адрес - {tokenAddress}");
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine("[ERROR] Ошибка парсинга JSON: {ex.Message}");
                    }
                }
            }
        }

        private string ExtractTokenAddress(string[] logs)
        {
            foreach (var log in logs)
            {
                if (log.Contains("create") && log.Contains("TokenMint"))
                {
                    var parts = log.Split(new[] { "TokenMint" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        var addressPart = parts[1].Trim().Split(' ')[0];
                        if (addressPart.Length == 44)
                            return addressPart;
                    }
                }
            }
            return null;
        }

        protected override void OnClosed(EventArgs e)
        {
            _cts?.Cancel();
            _wsClient?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закрытие окна", CancellationToken.None);
            Console.WriteLine("[DEBUG] Окно закрыто");
            base.OnClosed(e);
        }
    }

    public class JsonResponse
    {
        public string? Jsonrpc { get; set; }
        public int Id { get; set; }
        public Result? Result { get; set; }
    }

    public class Result
    {
        public Value? Value { get; set; }
    }

    public class Value
    {
        public string[]? Logs { get; set; }
        public string? Signature { get; set; }
    }

    public class TokenInfo : INotifyPropertyChanged
    {
        private string? _tokenAddress;
        private string? _developer;
        private double _marketCap;

        public string? TokenAddress
        {
            get => _tokenAddress;
            set { _tokenAddress = value; OnPropertyChanged(nameof(TokenAddress)); }
        }

        public string? Developer
        {
            get => _developer;
            set { _developer = value; OnPropertyChanged(nameof(Developer)); }
        }

        public double MarketCap
        {
            get => _marketCap;
            set { _marketCap = value; OnPropertyChanged(nameof(MarketCap)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}