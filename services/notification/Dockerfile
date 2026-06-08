FROM golang:1.26.1-alpine AS build
WORKDIR /app
 
COPY go.mod go.sum ./
RUN go mod download

COPY . .

RUN CGO_ENABLED=0 GOOS=linux go build -o /app/notification .

FROM alpine:3.23.3
RUN apk add --no-cache curl
COPY --from=build /app/notification ./notification
 
EXPOSE 8080

CMD ["./notification"]
 