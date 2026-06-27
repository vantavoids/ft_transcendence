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
        '-p 5672:5672 -p 15672:15672 ' +
        '-e RABBITMQ_DEFAULT_USER=' + MQ_USER + ' ' +
        '-e RABBITMQ_DEFAULT_PASS=' + MQ_PASS + ' ' +
        '-v $(pwd)/infra/rabbitmq/rabbitmq.conf:/etc/rabbitmq/rabbitmq.conf:ro ' +
        '-v rabbitmq_data:/var/lib/rabbitmq ' +
        'docker.io/rabbitmq:management-alpine'
    ),
    resource_deps=['dev-network'],
    labels=['infra'],
    links=['http://localhost:15672'],
)

local_resource(
    'minio',
    # bucket creation is folded into the server container via entrypoint.sh,
    # matching compose.yaml (avoids the podman one-shot --requires deadlock).
    serve_cmd=container_serve(DOCKER, FLAGS, 'minio',
        '--network ft_transcendence ' +
        '-p 9000:9000 -p 9001:9001 ' +
        '-e MINIO_ROOT_USER=' + MINIO_KEY + ' ' +
        '-e MINIO_ROOT_PASSWORD=' + MINIO_SECRET + ' ' +
        '-e MINIO_BUCKET=' + MINIO_BUCKET + ' ' +
        '-e MINIO_USER_BUCKET=' + MINIO_USER_BUCKET + ' ' +
        '-e MINIO_GUILD_BUCKET=' + MINIO_GUILD_BUCKET + ' ' +
        '-v minio_data:/data ' +
        '-v $(pwd)/infra/minio/entrypoint.sh:/usr/local/bin/minio-entrypoint.sh:ro ' +
        '--entrypoint sh docker.io/minio/minio /usr/local/bin/minio-entrypoint.sh'
    ),
    resource_deps=['dev-network'],
    labels=['infra'],
    links=['http://localhost:9001'],
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
        '-v $(pwd)/infra/coturn/turnserver.conf:/etc/coturn/turnserver.conf:ro ' +
        '-v $(pwd)/certs:/etc/coturn/certs:ro ' +
        'docker.io/coturn/coturn:alpine ' +
        '-c /etc/coturn/turnserver.conf ' +
        '--realm=' + TURN_REALM + ' ' +
        '--user=' + TURN_USERNAME + ':' + TURN_PASSWORD
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
        '--network host ' +
        '-v $(pwd)/infra/nginx/nginx.conf:/etc/nginx/nginx.conf:ro ' +
        '-v $(pwd)/infra/nginx/docs.html:/etc/nginx/docs.html:ro ' +
        '-v $(pwd)/certs:/etc/nginx/certs:ro ' +
        'docker.io/nginx:alpine'
    ),
    resource_deps=['cert-gen', 'dev-network', 'gateway', 'frontend'],
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
