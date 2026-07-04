load('./tiltlib.star', 'read_dotenv', 'detect_engine', 'run_flags', 'container_serve')

root_env     = read_dotenv('.env')
MQ_USER      = root_env.get('RABBITMQ_USER', 'guest')
MQ_PASS      = root_env.get('RABBITMQ_PASS', 'guest')
BASE_URL     = root_env.get('BASE_URL',     'https://localhost:1443')
BASE_API_URL = root_env.get('BASE_API_URL', 'https://localhost:1443/api')
MINIO_KEY    = root_env.get('MINIO_ACCESS_KEY', 'minioadmin')
MINIO_SECRET = root_env.get('MINIO_SECRET_KEY', 'minioadmin')
MINIO_BUCKET = root_env.get('MINIO_BUCKET',       'chat-attachments')
MINIO_USER_BUCKET  = root_env.get('MINIO_USER_BUCKET',  'user-avatars')
MINIO_GUILD_BUCKET = root_env.get('MINIO_GUILD_BUCKET', 'guild-icons')
TURN_REALM    = root_env.get('TURN_REALM',    'localhost')
TURN_USERNAME = root_env.get('TURN_USERNAME', 'ft_turn')
TURN_PASSWORD = root_env.get('TURN_PASSWORD', 'ft_turn')
DOCKER       = detect_engine()
FLAGS        = run_flags(DOCKER)

local_resource(
    'dev-network',
    cmd=DOCKER + ' network create ft_transcendence 2>/dev/null || true',
    labels=['infra'],
)

local_resource(
    'rabbitmq',
    serve_cmd=container_serve(DOCKER, FLAGS, 'rabbitmq',
        '--network ft_transcendence ' +
        '-p 127.0.0.1:5672:5672 -p 127.0.0.1:15672:15672 -p 127.0.0.1:15692:15692 ' +
        '-e RABBITMQ_DEFAULT_USER=' + MQ_USER + ' ' +
        '-e RABBITMQ_DEFAULT_PASS=' + MQ_PASS + ' ' +
        '-v $(pwd)/infra/rabbitmq/rabbitmq.conf:/etc/rabbitmq/rabbitmq.conf:ro ' +
        '-v $(pwd)/infra/rabbitmq/enabled_plugins:/etc/rabbitmq/enabled_plugins:ro ' +
        '-v rabbitmq_data:/var/lib/rabbitmq ' +
        'docker.io/rabbitmq:management-alpine'
    ),
    resource_deps=['dev-network'],
    labels=['infra'],
    # management UI serves under /rabbitmq (management.path_prefix in rabbitmq.conf)
    links=['http://localhost:15672/rabbitmq'],
)

local_resource(
    'minio',
    # bucket creation is folded into the server container via entrypoint.sh,
    # matching compose.yaml (avoids the podman one-shot --requires deadlock).
    serve_cmd=container_serve(DOCKER, FLAGS, 'minio',
        '--network ft_transcendence ' +
        '-p 127.0.0.1:9000:9000 -p 127.0.0.1:9001:9001 ' +
        '-e MINIO_ROOT_USER=' + MINIO_KEY + ' ' +
        '-e MINIO_ROOT_PASSWORD=' + MINIO_SECRET + ' ' +
        '-e MINIO_BUCKET=' + MINIO_BUCKET + ' ' +
        '-e MINIO_USER_BUCKET=' + MINIO_USER_BUCKET + ' ' +
        '-e MINIO_GUILD_BUCKET=' + MINIO_GUILD_BUCKET + ' ' +
        '-e MINIO_PROMETHEUS_AUTH_TYPE=public ' +
        # console is served through nginx at /minio (matches compose); this points
        # the console at that base so its assets/redirects resolve there. Note this
        # makes direct :9001 console access break, so the link below is the nginx one.
        '-e MINIO_BROWSER_REDIRECT_URL=' + BASE_URL + '/minio ' +
        '-v minio_data:/data ' +
        '-v $(pwd)/infra/minio/entrypoint.sh:/usr/local/bin/minio-entrypoint.sh:ro ' +
        '--entrypoint sh docker.io/minio/minio /usr/local/bin/minio-entrypoint.sh'
    ),
    resource_deps=['dev-network'],
    labels=['infra'],
    links=[BASE_URL + '/minio'],
)

local_resource(
    'coturn',
    # STUN/TURN for WebRTC. Host networking (like nginx) so the relay UDP range
    # and 3478/5349 bind directly. Realm + long-term user come from the root .env;
    # the rest of the config is in infra/coturn/turnserver.conf.
    serve_cmd=container_serve(DOCKER, FLAGS, 'coturn',
        '--network host ' +
        # run as root so coturn can read the 0600 TLS key in ./certs (see compose.yaml)
        '--user 0:0 ' +
        # entrypoint pins the relay/external IP to one reachable IPv4 (see
        # infra/coturn/entrypoint.sh); realm + user come from the environment.
        '-e TURN_REALM=' + TURN_REALM + ' ' +
        '-e TURN_USERNAME=' + TURN_USERNAME + ' ' +
        '-e TURN_PASSWORD=' + TURN_PASSWORD + ' ' +
        '-e TURN_RELAY_IP=' + root_env.get('TURN_RELAY_IP', '') + ' ' +
        '--entrypoint /usr/local/bin/coturn-entrypoint.sh ' +
        '-v $(pwd)/infra/coturn/turnserver.conf:/etc/coturn/turnserver.conf:ro ' +
        '-v $(pwd)/infra/coturn/entrypoint.sh:/usr/local/bin/coturn-entrypoint.sh:ro ' +
        '-v $(pwd)/certs:/etc/coturn/certs:ro ' +
        'docker.io/coturn/coturn:alpine'
    ),
    resource_deps=['cert-gen', 'dev-network'],
    labels=['infra'],
)

local_resource(
    'cert-gen',
    # host-side generation (openssl on the host) to mirror `make` and keep the
    # certs owned by the current user; nginx bind-mounts them below.
    cmd='sh infra/cert-gen/cert-gen.sh',
    labels=['infra'],
)

local_resource(
    'nginx',
    serve_cmd=container_serve(DOCKER, FLAGS, 'nginx',
        # host networking so nginx sees the real client IP for the gateway rate
        # limiter; app backends (gateway, frontend) are reached over unix sockets.
        '--network host ' +
        '-v $(pwd)/infra/nginx/nginx.conf:/etc/nginx/nginx.conf:ro ' +
        '-v $(pwd)/infra/nginx/docs.html:/etc/nginx/docs.html:ro ' +
        '-v $(pwd)/certs:/etc/nginx/certs:ro ' +
        '-v gateway_socket:/run/gateway ' +
        '-v frontend_socket:/run/frontend ' +
        'docker.io/nginx:alpine'
    ),
    resource_deps=['cert-gen', 'dev-network', 'gateway', 'frontend', 'frontend-socket'],
    labels=['infra'],
    links=['https://localhost:1443', 'https://localhost:1443/docs'],
)

include('./frontend/Tiltfile')
include('./services/auth/Tiltfile')
include('./services/chat/Tiltfile')
include('./services/gateway/Tiltfile')
include('./services/guild/Tiltfile')
include('./services/notification/Tiltfile')
include('./services/user/Tiltfile')

GRAFANA_USER = root_env.get('GRAFANA_ADMIN_USER', 'admin')
GRAFANA_PASS = root_env.get('GRAFANA_ADMIN_PASSWORD', 'admin')

local_resource(
    'prometheus',
    serve_cmd=container_serve(DOCKER, FLAGS, 'prometheus',
        '--network ft_transcendence ' +
        '-p 127.0.0.1:9090:9090 ' +
        '-v $(pwd)/infra/monitoring/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro ' +
        '-v prometheus_data:/prometheus ' +
        'docker.io/prom/prometheus:v2.55.1 ' +
        '--config.file=/etc/prometheus/prometheus.yml ' +
        '--storage.tsdb.path=/prometheus ' +
        '--storage.tsdb.retention.time=15d'
    ),
    resource_deps=['dev-network'],
    labels=['monitoring'],
    links=['http://localhost:9090'],
)

local_resource(
    'blackbox-exporter',
    serve_cmd=container_serve(DOCKER, FLAGS, 'blackbox-exporter',
        '--network ft_transcendence ' +
        '-v $(pwd)/infra/monitoring/blackbox/blackbox.yml:/etc/blackbox_exporter/config.yml:ro ' +
        'quay.io/prometheus/blackbox-exporter:v0.25.0'
    ),
    resource_deps=['dev-network'],
    labels=['monitoring'],
)

local_resource(
    'grafana',
    serve_cmd=container_serve(DOCKER, FLAGS, 'grafana',
        '--network ft_transcendence ' +
        # 3001 on the host: the frontend already owns 3000 (Grafana's container port)
        '-p 127.0.0.1:3001:3000 ' +
        '-e GF_SECURITY_ADMIN_USER=' + GRAFANA_USER + ' ' +
        '-e GF_SECURITY_ADMIN_PASSWORD=' + GRAFANA_PASS + ' ' +
        '-e GF_USERS_ALLOW_SIGN_UP=false ' +
        # reachable only through nginx TLS at /grafana (3001 is the proxy hop)
        '-e GF_SERVER_ROOT_URL=https://localhost:1443/grafana/ ' +
        '-e GF_SERVER_SERVE_FROM_SUB_PATH=true ' +
        '-v $(pwd)/infra/monitoring/grafana/provisioning:/etc/grafana/provisioning:ro ' +
        '-v $(pwd)/infra/monitoring/grafana/dashboards:/var/lib/grafana/dashboards:ro ' +
        '-v grafana_data:/var/lib/grafana ' +
        'docker.io/grafana/grafana:11.3.0'
    ),
    resource_deps=['prometheus'],
    labels=['monitoring'],
    links=['https://localhost:1443/grafana'],
)

for svc in ['auth', 'user', 'guild', 'notification']:
    svc_env = read_dotenv('services/' + svc + '/.env')
    dsn = ('postgresql://' + svc_env.get('POSTGRES_USER', svc) + ':' +
           svc_env.get('POSTGRES_PASSWORD', '') + '@' + svc + '-db:5432/' +
           svc_env.get('POSTGRES_DB', svc) + '?sslmode=disable')
    local_resource(
        svc + '-db-exporter',
        serve_cmd=container_serve(DOCKER, FLAGS, svc + '-db-exporter',
            '--network ft_transcendence ' +
            "-e 'DATA_SOURCE_NAME=" + dsn + "' " +
            'quay.io/prometheuscommunity/postgres-exporter:v0.15.0'
        ),
        resource_deps=[svc + '-db'],
        labels=['monitoring'],
    )
