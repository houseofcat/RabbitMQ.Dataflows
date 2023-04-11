# HouseofCat.RabbitMQ.Recoverable

Implementations of ChannelHost/Pool, ConnectionHost/Pool, Consumer and RabbitService which hook into Recovery events from the official RabbitMQ.Client library. Wherever possible, channels, connections and consumers will be reused rather than recreated as long as recovery was successful.