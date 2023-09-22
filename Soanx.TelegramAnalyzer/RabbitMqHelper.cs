using RabbitMQ.Client;
using Soanx.Repositories.Models;
using Soanx.TelegramAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static Soanx.CurrencyExchange.Models.DtoModels;

namespace Soanx.TelegramAnalyzer;
public class RabbitMqConnection {
    public RabbitMqCredentials MqCredentials { get; private set; }
    public IConnection Connection { get; private set; }
    public IModel Channel { get; private set; }

    public RabbitMqConnection(RabbitMqCredentials mqCredentials) {
        MqCredentials = mqCredentials;
        InitConnection();
    }

    private void InitConnection() {
        var factory = new ConnectionFactory() {
            HostName = MqCredentials.Hostname,
            Port = MqCredentials.Port,
            VirtualHost = MqCredentials.VirtualHost,
            UserName = MqCredentials.Username,
            Password = MqCredentials.Password
        };

        Connection = factory.CreateConnection();
        Channel = Connection.CreateModel();
    }
}

public abstract class QueueHelper {
    public RabbitMqConnection MqConnection { get; private set; }
    public QueueSettings QueueSettings { get; private set; }
    public QueueHelper(RabbitMqConnection mqConnection, QueueSettings queueSettings) {
        MqConnection = mqConnection;
        QueueSettings = queueSettings;
        Initialize();
    }

    private void Initialize() {
        MqConnection.Channel.ExchangeDeclare(QueueSettings.ExchangeName, "direct");
        MqConnection.Channel.QueueDeclare(
            queue: QueueSettings.QueueName,
            durable: QueueSettings.Durable,
            exclusive: QueueSettings.Exclusive,
            autoDelete: QueueSettings.AutoDelete,
            arguments: null);

        MqConnection.Channel.QueueBind(QueueSettings.QueueName, QueueSettings.ExchangeName, QueueSettings.RoutingKey);
    }

    protected void SendMessage<T>(T messageContent) {
        var jsonMessage = JsonSerializer.Serialize(messageContent);
        var body = Encoding.UTF8.GetBytes(jsonMessage);
        MqConnection.Channel.BasicPublish(exchange: QueueSettings.ExchangeName,
                                          routingKey: QueueSettings.RoutingKey,
                                          basicProperties: null,
                                          body: body);
    }
}

public class MessagesToAnalyzeQueue : QueueHelper {
    public MessagesToAnalyzeQueue(RabbitMqConnection mqConnection, QueueSettings queueSettings)
        : base(mqConnection, queueSettings) {
    }
    public void SendMessage(MessageToAnalyzing messageToAnalyze) {
        base.SendMessage<MessageToAnalyzing>(messageToAnalyze);
    }
}