package main

type secretFile struct {
	Encrypted string
	Plaintext string
}

const rootPath = "../../"
const secretmanDirPath = rootPath + "infra/secretman/"
const secretsDirPath = secretmanDirPath + "secrets/"

var secretFiles = []secretFile{
	{
		Encrypted: secretsDirPath + "root.env.crypt",
		Plaintext: rootPath + ".env",
	},
	{
		Encrypted: secretsDirPath + "front.env.crypt",
		Plaintext: rootPath + "frontend/.env",
	},
	{
		Encrypted: secretsDirPath + "auth.env.crypt",
		Plaintext: rootPath + "services/auth/.env",
	},
	{
		Encrypted: secretsDirPath + "chat.env.crypt",
		Plaintext: rootPath + "services/chat/.env",
	},
	{
		Encrypted: secretsDirPath + "gateway.env.crypt",
		Plaintext: rootPath + "services/gateway/.env",
	},
	{
		Encrypted: secretsDirPath + "guild.env.crypt",
		Plaintext: rootPath + "services/guild/.env",
	},
	{
		Encrypted: secretsDirPath + "notification.env.crypt",
		Plaintext: rootPath + "services/notification/.env",
	},
	{
		Encrypted: secretsDirPath + "user.env.crypt",
		Plaintext: rootPath + "services/user/.env",
	},
}
