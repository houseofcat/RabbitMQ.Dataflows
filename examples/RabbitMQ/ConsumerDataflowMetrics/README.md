# Setup - Non-Docker

Download Prometheus and Grafana binary.  

Configure Prometheus YML (in bin folder)  
eg.)
```yml
# my global config
global:
  scrape_interval:     1s # Set the scrape interval to every 15 seconds. Default is every 1 minute.
  evaluation_interval: 1s # Evaluate rules every 15 seconds. The default is every 1 minute.
  # scrape_timeout is set to the global default (10s).

# Alertmanager configuration
alerting:
  alertmanagers:
  - static_configs:
    - targets:
      # - alertmanager:9093

# Load rules once and periodically evaluate them according to the global 'evaluation_interval'.
rule_files:
  # - "first_rules.yml"
  # - "second_rules.yml"

# A scrape configuration containing exactly one endpoint to scrape:
# Here it's Prometheus itself.
scrape_configs:
  # The job name is added as a label `job=<job_name>` to any timeseries scraped from this config.
  - job_name: 'prometheus'

    # metrics_path defaults to '/metrics'
    # scheme defaults to 'http'.

    static_configs:
    - targets: ['localhost:9090']

  ### OUR STUFF
  - job_name: 'ConsumerWorkflowMetrics'
    static_configs:
      - targets: ['localhost:5000']
    metrics_path: /metrics

```

Run Prometheus  
eg.)  
```posh
cd "C:\prometheus"
.\prometheus.exe
```

eg.)
```posh
cd "C:\grafana\bin"
.\grafana-server.exe
```

# Default URLs
* localhost:5000 - this application (Kestrel), /metrics is the endpoint  
* localhost:3000 - Grafana, admin/admin  
* localhost:9090 - Prometheus  
* localhost:15672 - RabbitMQ, guest/guest  

# Appsettings.Json
```json
{
  "HouseofCat": {
    "ConsumerWorkflowService": {
      "WorkflowName": "ConsumerWorkflowMetrics",
      "ConsumerName": "ConsumerFromConfig",
      "ConsumerCount": 3,
      "MaxDoP": 4,
      "EnsureOrdered": false,
      "Capacity": 200,
      "SimulateIODelay": false,
      "MinIODelay": 40,
      "MaxIODelay": 60,
      "LogStepOutcomes": false
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}
```