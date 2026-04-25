load('./tiltlib.star', 'read_dotenv', 'detect_engine', 'run_flags', 'container_serve')

root_env     = read_dotenv('.env')
MQ_USER      = root_env.get('RABBITMQ_USER', 'guest')
MQ_PASS      = root_env.get('RABBITMQ_PASS', 'guest')
BASE_URL     = root_env.get('BASE_URL',     'https://localhost:1443')
BASE_API_URL = root_env.get('BASE_API_URL', 'https://localhost:1443/api')
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
    'cert-gen',
    cmd=(
        DOCKER + ' run ' + FLAGS + '--rm ' +
        '-v certs:/certs ' +
        '--entrypoint sh docker.io/alpine/openssl ' +
        '-c "rm -f /certs/key.pem /certs/cert.pem && ' +
        'openssl req -x509 -newkey rsa:4096 -nodes ' +
        '-keyout /certs/key.pem -out /certs/cert.pem ' +
        '-days 365 -subj \'/CN=localhost\' ' +
        '-addext \'subjectAltName=DNS:localhost\'"'
    ),
    labels=['infra'],
)

local_resource(
    'nginx',
    serve_cmd=container_serve(DOCKER, FLAGS, 'nginx',
        '--network ft_transcendence ' +
        '-p 1080:80 ' +
        '-p 1443:443 ' +
        '-v $(pwd)/infra/nginx/nginx.conf:/etc/nginx/nginx.conf:ro ' +
        '-v $(pwd)/infra/nginx/docs.html:/etc/nginx/docs.html:ro ' +
        '-v certs:/etc/nginx/certs:ro ' +
        'docker.io/nginx:alpine'
    ),
    resource_deps=['cert-gen', 'dev-network', 'gateway'],
    labels=['infra'],
    links=['https://localhost:1443', 'https://localhost:1443/docs'],
)

include('./services/auth/Tiltfile')
include('./services/chat/Tiltfile')
include('./services/gateway/Tiltfile')
include('./services/guild/Tiltfile')
include('./services/notification/Tiltfile')
include('./services/user/Tiltfile')
