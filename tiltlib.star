def detect_engine():
    result = str(local(
        'if which podman >/dev/null 2>&1; then echo podman; ' +
        'elif which docker >/dev/null 2>&1; then echo docker; fi',
        quiet = True,
    )).strip()
    if result == '':
        fail('Neither docker nor podman found in PATH')
    return result

def run_flags(engine):
    if engine == 'podman':
        return '--security-opt label=disable '
    return ''

def container_serve(engine, flags, name, run_args):
    cleanup = engine + ' rm -f ' + name + ' 2>/dev/null || true'
    watchdog = 'setsid sh -c "while kill -0 $PARENT 2>/dev/null; do sleep 1; done; ' + cleanup + '" &'
    return (
        'PARENT=$PPID; ' + watchdog + ' ' +
        cleanup + '; ' +
        engine + ' run ' + flags + '--name ' + name + ' --rm ' + run_args
    )

def read_dotenv(path):
    env = {}
    for line in str(read_file(path)).splitlines():
        line = line.strip()
        if line and not line.startswith('#') and '=' in line:
            k, _, v = line.partition('=')
            v = v.strip()
            if len(v) >= 2 and v[0] == v[-1] and v[0] in ('"', "'"):
                v = v[1:-1]
            env[k.strip()] = v
    return env
