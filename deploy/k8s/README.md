# Ручне розгортання в Kubernetes

Тут лежать власні маніфести для `gateway` і `registry-api` (сервіс реєстру пакетів). Кожен сервіс має
Deployment із двома репліками та Service типу ClusterIP, спільний `configmap.yaml` містить відкриті
налаштування, а секрети в репозиторій не потрапляють, бо їх створюють командами `kubectl create secret`, які
наведено нижче, і значення цих команд приходять зі змінних середовища. Кластер – окремий профіль minikube
`devhub`, тож кожна команда явно називає `--context devhub`.

## Образи

1. Зібрати образи з кореня репозиторію:

```bash
docker build -t registry-api:v1 -f Services/Registry/Registry.Api/Dockerfile .
docker build -t gateway:v1 -f Platform.ApiGateway/Dockerfile .
```

2. Передати їх у кластер. Профіль `devhub` працює на containerd і локальних образів Docker не бачить:

```bash
minikube -p devhub image load registry-api:v1
minikube -p devhub image load gateway:v1
```

Обидва Dockerfile багатоетапні. Спершу вони копіюють лише файли `.csproj` і виконують `dotnet restore`, тому
після зміни коду Docker бере цей шар з кешу й залежності вдруге не завантажує. Збирання йде в образі `sdk:9.0`,
у фінальний образ `aspnet:9.0` потрапляє тільки результат публікації, а застосунок слухає порт 8080. Маніфести
вказують `imagePullPolicy: IfNotPresent`, тож кластер бере завантажений образ і до реєстру не звертається.

## Бази даних

`registry-api` потребує SQL Server, Redis і RabbitMQ. Ручне розгортання використовує контейнери `devhub-sql`,
`devhub-redis` і `devhub-rabbitmq`, які створює AppHost. Їхні порти відкриті лише на `127.0.0.1`, тому ці
контейнери входять ще й до мережі Docker профілю minikube, і поди звертаються до них за іменем.

```bash
docker network connect devhub devhub-sql
docker network connect devhub devhub-redis
docker network connect devhub devhub-rabbitmq
```

## Секрети (kubectl create secret)

У YAML жодного значення немає. Паролі ті самі, що в секретах
користувача AppHost (розділ «Секрети й запуск» кореневого README): `SQL_PASSWORD` – `Parameters:sql-password`,
`REDIS_PASSWORD`, `MESSAGING_PASSWORD`, `JWT_SECRET` – відповідні `Parameters:*`.

```bash
export REGISTRY_DB="Server=devhub-sql,1433;User ID=sa;Password=$SQL_PASSWORD;Database=DevHub_Registry;TrustServerCertificate=true"
export REGISTRY_REDIS="devhub-redis:6379,password=$REDIS_PASSWORD"
export REGISTRY_MESSAGING="amqp://guest:$MESSAGING_PASSWORD@devhub-rabbitmq:5672"

kubectl --context devhub create secret generic registry-secrets \
  --from-literal=ConnectionStrings__RegistryDb="$REGISTRY_DB" \
  --from-literal=ConnectionStrings__redis="$REGISTRY_REDIS" \
  --from-literal=ConnectionStrings__messaging="$REGISTRY_MESSAGING" \
  --from-literal=Jwt__SecretKey="$JWT_SECRET"

kubectl --context devhub create secret generic gateway-secrets \
  --from-literal=Jwt__SecretKey="$JWT_SECRET"
```

Секрет створюють саме командою `create`. Якщо згенерувати YAML і передати його в `kubectl apply`, Kubernetes
збереже повне значення ще раз в анотації `kubectl.kubernetes.io/last-applied-configuration`. Дані в Secret
лише закодовано в Base64, а не зашифровано, і `kubectl --context devhub get secret gateway-secrets -o yaml`
показує рядок, який `base64 -d` повертає у відкритий вигляд.

## ConfigMap і маніфести

```bash
kubectl --context devhub apply -f deploy/k8s/configmap.yaml
kubectl --context devhub apply -f deploy/k8s/registry-api-deployment.yaml -f deploy/k8s/registry-api-service.yaml
kubectl --context devhub apply -f deploy/k8s/gateway-deployment.yaml -f deploy/k8s/gateway-service.yaml
kubectl --context devhub get pods,services
```

Контейнер отримує ConfigMap і Secret через `envFrom` (`configMapRef`, `secretRef`), тож їхні ключі стають
змінними середовища. `gateway-config` передає Gateway адресу `services__registry-api__http__0: "http://registry-api"`.
У такому самому форматі Aspire передавав адреси для service discovery, тому код Gateway лишився тим самим;
`registry-api` – DNS-ім’я Service, яке вбудований DNS Kubernetes перетворює на його ClusterIP. Обидва сервіси працюють у середовищі
`Staging`. Це не Development, тож проби підтверджують, що `/health/live` і `/health/ready` зареєстровано в усіх
середовищах, а початкові пакети реєстру однаково завантажуються. Зміну в ConfigMap контейнер бачить лише після
`kubectl --context devhub rollout restart deploy/gateway`.

## Перевірка

1. Прокинути порт Gateway і запитати пакети:

```bash
kubectl --context devhub port-forward service/gateway 8080:80
curl -H "Authorization: Bearer $TOKEN" http://localhost:8080/api/packages
```

Без заголовка `Authorization` Gateway відповідає 401. Токен видає Auth.Api платформи, запущеної через Aspire
(`POST /auth/login`), а Gateway перевіряє його підпис тим самим ключем `Jwt__SecretKey`. У журналі Gateway видно маршрут
`registry` і адресу `http://registry-api/`.

2. Видалити один под: `kubectl --context devhub delete pod <ім’я поду registry-api>`. Deployment за кілька секунд
створює новий, бо стежить, щоб працювали дві репліки.

3. Зупинити базу: `docker stop devhub-sql`. Поди `registry-api` лишаються в стані Running, але стають 0/1.
Readiness-проба `/health/ready` перевіряє `registry-db` і повертає 503, тому Service перестає слати їм запити.
Liveness-проба `/health/live` перевіряє лише сам процес, отже контейнер не перезапускається, і лічильник
перезапусків лишається нульовим. Після `docker start devhub-sql` обидва поди знову готові.
