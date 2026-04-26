# SEED Visual Library — Icon & Module Catalog

**Date** : 2026-04-25
**Status** : Spec — implementation lives in FORGE (`forge/src/Forge.Godot/Visual/SeedRenderer/`)
**Scope** : Catalog of icons + 3D module shapes that map DSL slot values (targets, modifiers, entities) to recognizable visual representations.

---

## Purpose

When a `.dna` file references `<server>`, `<modem>`, `<file>`, the FORGE renderer should display **a server**, **a modem**, **a file** — not a generic cube with text. Each entry below maps a concept to:

- **Slug** : canonical identifier used in DSL slots (e.g., `<server-web>`)
- **Aliases** : alternative DSL slot values that resolve to the same icon (matched via FORGE-side fuzzy/synonym table)
- **2D icon** : `IconLibrary/<slug>.svg` — used on module faces, in panels, in lists
- **3D shape** : `ModuleShapes/<PascalCase>.tscn` — used in the FORGE 3D scene for full-fidelity rendering
- **Recommended color** : palette hint (overridable by category color from verb)

## Asset conventions

- **2D icons** : SVG, 64×64 px viewBox, monochrome with `currentColor` for tinting, single-path where possible
- **3D shapes** : Godot `.tscn` with single root Node3D, ~1m bounding box, materials use shader globals so themes can recolor
- **Naming** : `kebab-case` for slugs and SVGs, `PascalCase` for `.tscn` files

## How matching works

1. Parser reads slot value (e.g., `<modem>`, `<modem-cable>`, `<adsl-modem>`)
2. FORGE renderer looks up slug in the catalog
3. If exact match : load the asset
4. If alias match : load the parent asset
5. If no match : fallback to category default (cube + text label) — same as current FORGE module

---

## 1. Hardware — Computers & Devices

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `computer-desktop` | `pc`, `desktop`, `tower` | `computer-desktop.svg` | `ComputerDesktop.tscn` | Tower box + monitor (CRT-style retro or modern flat) |
| `computer-laptop` | `laptop`, `notebook`, `macbook` | `computer-laptop.svg` | `ComputerLaptop.tscn` | Open clamshell with screen tilted |
| `computer-mainframe` | `mainframe`, `bigiron` | `computer-mainframe.svg` | `ComputerMainframe.tscn` | Tall rack with blinking lights |
| `phone-mobile` | `phone`, `smartphone`, `mobile`, `iphone`, `android` | `phone-mobile.svg` | `PhoneMobile.tscn` | Vertical slab with rounded corners |
| `phone-landline` | `landline`, `voip-phone` | `phone-landline.svg` | `PhoneLandline.tscn` | Handset on cradle |
| `tablet` | `ipad`, `pad` | `tablet.svg` | `Tablet.tscn` | Flat thin slab, larger than phone |
| `watch-smart` | `smartwatch`, `wearable` | `watch-smart.svg` | `WatchSmart.tscn` | Square face with strap |
| `tv-screen` | `tv`, `monitor`, `display`, `screen` | `tv-screen.svg` | `TvScreen.tscn` | Flat panel on stand |
| `iot-sensor` | `sensor`, `iot`, `device` | `iot-sensor.svg` | `IotSensor.tscn` | Small box with pulsing antenna |
| `raspberry-pi` | `rpi`, `sbc`, `single-board` | `raspberry-pi.svg` | `RaspberryPi.tscn` | Bare PCB with green tint |
| `arduino` | `microcontroller`, `mcu` | `arduino.svg` | `Arduino.tscn` | Small blue PCB |
| `drone` | `uav`, `quadcopter` | `drone.svg` | `Drone.tscn` | 4-rotor frame, top-down view |
| `printer` | `print` | `printer.svg` | `Printer.tscn` | Box with paper tray |
| `scanner` | `scan` | `scanner.svg` | `Scanner.tscn` | Flat unit with lid |
| `camera` | `webcam`, `cam`, `video-cam` | `camera.svg` | `Camera.tscn` | Lens cylinder + body |

---

## 2. Hardware — Network Equipment

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `modem` | `modem-dsl`, `modem-cable`, `modem-fiber` | `modem.svg` | `Modem.tscn` | Horizontal box with antenna + 4 LEDs blinking |
| `router` | `wifi-router`, `gateway-router` | `router.svg` | `Router.tscn` | Box with 2-4 antennas |
| `switch-network` | `switch`, `ethernet-switch` | `switch-network.svg` | `SwitchNetwork.tscn` | 1U rack unit with port row |
| `hub-network` | `hub` | `hub-network.svg` | `HubNetwork.tscn` | Same as switch but simpler |
| `firewall` | `fw`, `pfsense`, `barrier` | `firewall.svg` | `Firewall.tscn` | Brick wall with flame motif |
| `load-balancer` | `lb`, `haproxy`, `nginx-lb` | `load-balancer.svg` | `LoadBalancer.tscn` | Diamond splitter shape |
| `proxy` | `reverse-proxy`, `forward-proxy` | `proxy.svg` | `Proxy.tscn` | Funnel shape |
| `vpn` | `vpn-tunnel`, `tunnel`, `wireguard`, `openvpn` | `vpn.svg` | `Vpn.tscn` | Tunnel with lock |
| `access-point` | `wifi-ap`, `ap` | `access-point.svg` | `AccessPoint.tscn` | Disc with radial waves |
| `nic` | `network-card`, `ethernet-port` | `nic.svg` | `Nic.tscn` | RJ-45 port |

---

## 3. Hardware — Servers & Storage

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `server-rack` | `server`, `rack`, `1u`, `2u` | `server-rack.svg` | `ServerRack.tscn` | Standard 1U rack server with vents |
| `server-tower` | `server-tower`, `tower-server` | `server-tower.svg` | `ServerTower.tscn` | Vertical case |
| `server-blade` | `blade`, `blade-server` | `server-blade.svg` | `ServerBlade.tscn` | Thin slot card |
| `server-web` | `web-server`, `nginx`, `apache`, `iis`, `httpd` | `server-web.svg` | `ServerWeb.tscn` | Server + globe icon |
| `server-mail` | `mail-server`, `smtp`, `imap`, `postfix` | `server-mail.svg` | `ServerMail.tscn` | Server + envelope icon |
| `server-dns` | `dns`, `bind`, `nameserver` | `server-dns.svg` | `ServerDns.tscn` | Server + ?→IP arrow |
| `server-ftp` | `ftp`, `sftp`, `ftps` | `server-ftp.svg` | `ServerFtp.tscn` | Server + folder transfer |
| `server-game` | `game-server`, `gameserver` | `server-game.svg` | `ServerGame.tscn` | Server + controller icon |
| `data-center` | `dc`, `colocation`, `colo` | `data-center.svg` | `DataCenter.tscn` | Building with rack rows |
| `disk-hdd` | `hdd`, `disk`, `hard-drive` | `disk-hdd.svg` | `DiskHdd.tscn` | Cylinder platter |
| `disk-ssd` | `ssd`, `nvme`, `flash-drive` | `disk-ssd.svg` | `DiskSsd.tscn` | Flat chip rectangle |
| `nas` | `nas-box`, `synology`, `network-storage` | `nas.svg` | `Nas.tscn` | Stacked drive bays |
| `tape-backup` | `tape`, `lto`, `backup-tape` | `tape-backup.svg` | `TapeBackup.tscn` | Cassette spool |
| `usb-drive` | `usb`, `thumb-drive`, `flash` | `usb-drive.svg` | `UsbDrive.tscn` | Stick with cap |

---

## 4. Storage — Databases & Data Stores

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `db-sql` | `sql`, `rdbms`, `mysql`, `postgres`, `postgresql`, `mssql`, `mariadb`, `oracle` | `db-sql.svg` | `DbSql.tscn` | Classic 3-disc cylinder (tableau) |
| `db-nosql-document` | `mongo`, `mongodb`, `couchdb`, `documentdb` | `db-nosql-document.svg` | `DbNosqlDocument.tscn` | Cylinder with leaf/document overlay |
| `db-nosql-keyvalue` | `redis`, `memcached`, `dynamodb`, `keyvalue` | `db-nosql-keyvalue.svg` | `DbNosqlKeyvalue.tscn` | Cylinder with key icon |
| `db-nosql-graph` | `neo4j`, `graph-db`, `arangodb` | `db-nosql-graph.svg` | `DbNosqlGraph.tscn` | Connected nodes graph |
| `db-nosql-column` | `cassandra`, `hbase`, `column-store` | `db-nosql-column.svg` | `DbNosqlColumn.tscn` | Cylinder with column bars |
| `db-timeseries` | `tsdb`, `influxdb`, `timescale`, `prometheus-tsdb` | `db-timeseries.svg` | `DbTimeseries.tscn` | Cylinder with timeline |
| `db-vector` | `vector-db`, `pinecone`, `weaviate`, `qdrant`, `embeddings-store` | `db-vector.svg` | `DbVector.tscn` | Cylinder with vector arrow |
| `db-search` | `elastic`, `elasticsearch`, `solr`, `meilisearch`, `typesense` | `db-search.svg` | `DbSearch.tscn` | Cylinder with magnifying glass |
| `bucket-s3` | `s3`, `bucket`, `object-storage`, `gcs`, `azure-blob`, `r2` | `bucket-s3.svg` | `BucketS3.tscn` | Bucket shape with content inside |
| `cache` | `cache-layer`, `varnish`, `cdn-cache` | `cache.svg` | `Cache.tscn` | Stacked tiles with lightning |
| `cdn` | `cloudflare-cdn`, `fastly`, `cloudfront`, `akamai` | `cdn.svg` | `Cdn.tscn` | Globe with edge nodes |
| `queue` | `message-queue`, `rabbitmq`, `sqs`, `kafka`, `redis-queue` | `queue.svg` | `Queue.tscn` | Pipe with stacked items |
| `topic-pubsub` | `pubsub`, `topic`, `kafka-topic`, `nats` | `topic-pubsub.svg` | `TopicPubsub.tscn` | Hub with radiating arrows |
| `stream` | `data-stream`, `kinesis`, `event-stream` | `stream.svg` | `Stream.tscn` | Flowing waves |
| `data-lake` | `lake`, `datalake`, `delta-lake` | `data-lake.svg` | `DataLake.tscn` | Cylinder pool wide & shallow |
| `data-warehouse` | `dw`, `dwh`, `snowflake`, `bigquery`, `redshift` | `data-warehouse.svg` | `DataWarehouse.tscn` | Big building with shelves |

---

## 5. Files & Content

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `file-generic` | `file`, `document` | `file-generic.svg` | `FileGeneric.tscn` | Page with folded corner |
| `file-text` | `txt`, `text-file` | `file-text.svg` | `FileText.tscn` | Page with text lines |
| `file-code` | `code`, `source-file`, `script` | `file-code.svg` | `FileCode.tscn` | Page with `< />` |
| `file-config` | `config`, `yaml`, `toml`, `ini`, `dotenv` | `file-config.svg` | `FileConfig.tscn` | Page with gear |
| `file-json` | `json` | `file-json.svg` | `FileJson.tscn` | Page with `{}` |
| `file-xml` | `xml` | `file-xml.svg` | `FileXml.tscn` | Page with `<>` |
| `file-csv` | `csv`, `tsv` | `file-csv.svg` | `FileCsv.tscn` | Page with grid |
| `file-markdown` | `md`, `markdown` | `file-markdown.svg` | `FileMarkdown.tscn` | Page with `M↓` |
| `file-pdf` | `pdf` | `file-pdf.svg` | `FilePdf.tscn` | Page with red PDF tag |
| `file-image` | `image`, `img`, `png`, `jpg`, `jpeg`, `webp`, `gif`, `svg` | `file-image.svg` | `FileImage.tscn` | Page with mountain icon |
| `file-video` | `video`, `mp4`, `mov`, `avi`, `mkv` | `file-video.svg` | `FileVideo.tscn` | Page with play triangle |
| `file-audio` | `audio`, `mp3`, `wav`, `flac`, `ogg` | `file-audio.svg` | `FileAudio.tscn` | Page with waveform |
| `file-archive` | `archive`, `zip`, `tar`, `gz`, `7z`, `rar` | `file-archive.svg` | `FileArchive.tscn` | Page with zipper |
| `file-spreadsheet` | `xlsx`, `ods`, `excel`, `numbers` | `file-spreadsheet.svg` | `FileSpreadsheet.tscn` | Page with grid + green tag |
| `file-presentation` | `pptx`, `keynote`, `slides` | `file-presentation.svg` | `FilePresentation.tscn` | Page with bar chart |
| `file-doc` | `doc`, `docx`, `word` | `file-doc.svg` | `FileDoc.tscn` | Page with W |
| `file-log` | `log`, `logfile` | `file-log.svg` | `FileLog.tscn` | Page with timestamp lines |
| `file-binary` | `bin`, `exe`, `dll`, `so`, `dylib` | `file-binary.svg` | `FileBinary.tscn` | Page with `01010` |
| `folder` | `directory`, `dir`, `dossier` | `folder.svg` | `Folder.tscn` | Tabbed folder |
| `folder-shared` | `share`, `network-folder` | `folder-shared.svg` | `FolderShared.tscn` | Folder with shared overlay |

---

## 6. Software — Apps & Services

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `app-web` | `webapp`, `web-app`, `spa`, `website` | `app-web.svg` | `AppWeb.tscn` | Browser frame with content |
| `app-mobile` | `mobile-app`, `ios-app`, `android-app` | `app-mobile.svg` | `AppMobile.tscn` | Phone frame with app grid |
| `app-desktop` | `desktop-app`, `native-app` | `app-desktop.svg` | `AppDesktop.tscn` | Window frame with title bar |
| `app-cli` | `cli`, `terminal-app`, `console-app` | `app-cli.svg` | `AppCli.tscn` | Terminal box with `>` prompt |
| `daemon` | `service`, `background-process` | `daemon.svg` | `Daemon.tscn` | Cog with running indicator |
| `microservice` | `service`, `µ-service` | `microservice.svg` | `Microservice.tscn` | Small block with input/output ports |
| `monolith` | `monolithic`, `single-app` | `monolith.svg` | `Monolith.tscn` | Big block with many ports |
| `container` | `docker`, `container-image`, `oci` | `container.svg` | `Container.tscn` | Shipping container |
| `vm` | `virtual-machine`, `vmware`, `hyper-v`, `qemu` | `vm.svg` | `Vm.tscn` | Box-in-box (host + guest) |
| `pod-k8s` | `pod`, `kubernetes-pod`, `k8s` | `pod-k8s.svg` | `PodK8s.tscn` | Hexagon with 1-3 container slots |
| `cluster-k8s` | `cluster`, `k8s-cluster`, `kubernetes-cluster` | `cluster-k8s.svg` | `ClusterK8s.tscn` | Group of pods on platform |
| `function-lambda` | `lambda`, `serverless`, `cloud-function`, `faas` | `function-lambda.svg` | `FunctionLambda.tscn` | λ symbol on platform |
| `static-site` | `static`, `jekyll`, `hugo`, `gatsby`, `nextjs-static` | `static-site.svg` | `StaticSite.tscn` | Page with HTML tag |
| `worker` | `background-worker`, `job-runner`, `celery` | `worker.svg` | `Worker.tscn` | Hard hat on cog |
| `cron` | `scheduled-job`, `crontab`, `timer-job` | `cron.svg` | `Cron.tscn` | Clock face with arrow |
| `scheduler` | `task-scheduler` | `scheduler.svg` | `Scheduler.tscn` | Calendar with clock |

---

## 7. APIs & Communication

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `api-rest` | `rest`, `restful`, `http-api` | `api-rest.svg` | `ApiRest.tscn` | Connector hub with REST verbs |
| `api-graphql` | `graphql`, `gql` | `api-graphql.svg` | `ApiGraphql.tscn` | GraphQL pink rocket icon |
| `api-grpc` | `grpc`, `protobuf` | `api-grpc.svg` | `ApiGrpc.tscn` | Hexagon with arrow |
| `api-soap` | `soap`, `wsdl` | `api-soap.svg` | `ApiSoap.tscn` | XML envelope |
| `webhook` | `webhook-in`, `webhook-out` | `webhook.svg` | `Webhook.tscn` | Hook + lightning |
| `websocket` | `ws`, `wss`, `socket` | `websocket.svg` | `Websocket.tscn` | Two-way arrow socket |
| `mqtt` | `mqtt-broker`, `iot-protocol` | `mqtt.svg` | `Mqtt.tscn` | Pub/sub fan with M |
| `email-out` | `email`, `smtp-out`, `mail-send` | `email-out.svg` | `EmailOut.tscn` | Envelope with up arrow |
| `email-in` | `mail-receive`, `imap-in`, `pop3-in` | `email-in.svg` | `EmailIn.tscn` | Envelope with down arrow |
| `sms` | `text-message`, `twilio-sms` | `sms.svg` | `Sms.tscn` | Speech bubble with phone |
| `slack-message` | `slack`, `slack-msg`, `slack-channel` | `slack-message.svg` | `SlackMessage.tscn` | Slack hash symbol |
| `discord-message` | `discord` | `discord-message.svg` | `DiscordMessage.tscn` | Discord controller-face |
| `teams-message` | `teams`, `ms-teams` | `teams-message.svg` | `TeamsMessage.tscn` | T in box |
| `push-notification` | `push`, `notif`, `notification` | `push-notification.svg` | `PushNotification.tscn` | Bell with dot |
| `voice-call` | `call`, `phone-call`, `voip-call` | `voice-call.svg` | `VoiceCall.tscn` | Handset with waves |
| `video-call` | `videoconf`, `meeting`, `zoom-call` | `video-call.svg` | `VideoCall.tscn` | Camera with people |

---

## 8. Auth, Security & Identity

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `user` | `account`, `person`, `client` | `user.svg` | `User.tscn` | Head + shoulders silhouette |
| `user-admin` | `admin`, `root`, `superuser` | `user-admin.svg` | `UserAdmin.tscn` | User with crown |
| `user-guest` | `guest`, `anonymous` | `user-guest.svg` | `UserGuest.tscn` | User with `?` |
| `team` | `group`, `org`, `organization` | `team.svg` | `Team.tscn` | 3 user silhouettes |
| `role` | `permission`, `rbac` | `role.svg` | `Role.tscn` | Badge with star |
| `auth-jwt` | `jwt`, `bearer-token` | `auth-jwt.svg` | `AuthJwt.tscn` | Key with `JWT` text |
| `auth-oauth` | `oauth`, `oauth2`, `sso`, `openid` | `auth-oauth.svg` | `AuthOauth.tscn` | Circular handshake |
| `auth-saml` | `saml`, `sso-enterprise` | `auth-saml.svg` | `AuthSaml.tscn` | SAML hexagon |
| `auth-api-key` | `api-key`, `apikey`, `secret-key` | `auth-api-key.svg` | `AuthApiKey.tscn` | Key with serial |
| `auth-mfa` | `mfa`, `2fa`, `totp`, `webauthn` | `auth-mfa.svg` | `AuthMfa.tscn` | Phone with shield |
| `certificate` | `cert`, `tls-cert`, `ssl-cert`, `x509` | `certificate.svg` | `Certificate.tscn` | Ribbon document |
| `vault` | `secret-vault`, `keyvault`, `hashicorp-vault` | `vault.svg` | `Vault.tscn` | Bank vault door |
| `password` | `pwd`, `passphrase` | `password.svg` | `Password.tscn` | Asterisks `***` |
| `session` | `cookie-session`, `session-token` | `session.svg` | `Session.tscn` | Cookie circle |
| `audit-log` | `audit`, `compliance-log`, `siem` | `audit-log.svg` | `AuditLog.tscn` | Document with magnifying glass |

---

## 9. Monitoring & Observability

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `dashboard` | `dash`, `grafana-dashboard` | `dashboard.svg` | `Dashboard.tscn` | Gauge cluster panel |
| `chart-line` | `chart`, `line-chart`, `trend` | `chart-line.svg` | `ChartLine.tscn` | Rising line graph |
| `chart-bar` | `bar-chart`, `histogram` | `chart-bar.svg` | `ChartBar.tscn` | Vertical bars |
| `chart-pie` | `pie-chart`, `donut-chart` | `chart-pie.svg` | `ChartPie.tscn` | Pie segments |
| `metric` | `kpi`, `gauge`, `counter` | `metric.svg` | `Metric.tscn` | Speedometer dial |
| `log-stream` | `logs`, `loki`, `splunk` | `log-stream.svg` | `LogStream.tscn` | Scrolling lines |
| `trace-distributed` | `trace`, `span`, `jaeger`, `zipkin` | `trace-distributed.svg` | `TraceDistributed.tscn` | Connected timeline bars |
| `alert` | `alarm`, `pagerduty-alert`, `opsgenie` | `alert.svg` | `Alert.tscn` | Bell with red dot |
| `health-check` | `healthcheck`, `liveness`, `readiness` | `health-check.svg` | `HealthCheck.tscn` | Heart with pulse |
| `error` | `exception`, `bug`, `crash` | `error.svg` | `Error.tscn` | Red triangle with `!` |
| `warning` | `warn`, `caution` | `warning.svg` | `Warning.tscn` | Yellow triangle with `!` |
| `info` | `notice` | `info.svg` | `Info.tscn` | Blue circle with `i` |
| `success` | `ok`, `passed`, `green` | `success.svg` | `Success.tscn` | Green circle with check |

---

## 10. AI / ML

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `llm` | `claude`, `gpt`, `model-chat`, `ai-model` | `llm.svg` | `Llm.tscn` | Brain with chat bubble |
| `model-embedding` | `embedding-model`, `embedder` | `model-embedding.svg` | `ModelEmbedding.tscn` | Brain with vector arrow |
| `model-image-gen` | `image-gen`, `dalle`, `midjourney`, `sdxl` | `model-image-gen.svg` | `ModelImageGen.tscn` | Brain with painting |
| `model-asr` | `whisper`, `speech-to-text`, `stt` | `model-asr.svg` | `ModelAsr.tscn` | Brain with sound wave |
| `model-tts` | `text-to-speech`, `tts`, `elevenlabs` | `model-tts.svg` | `ModelTts.tscn` | Brain with speaker |
| `prompt` | `system-prompt`, `instruction` | `prompt.svg` | `Prompt.tscn` | Speech bubble with `>` |
| `agent` | `ai-agent`, `assistant`, `bot-ai` | `agent.svg` | `Agent.tscn` | Robot avatar |
| `tool-use` | `function-call`, `mcp-tool` | `tool-use.svg` | `ToolUse.tscn` | Wrench |
| `mcp-server` | `mcp`, `model-context-protocol` | `mcp-server.svg` | `McpServer.tscn` | Server with brain plug |
| `training-data` | `dataset`, `corpus`, `training-set` | `training-data.svg` | `TrainingData.tscn` | Stack of labeled cards |
| `embedding-store` | `vector-store` | `embedding-store.svg` | `EmbeddingStore.tscn` | DB cylinder with vectors |
| `rag-pipeline` | `rag`, `retrieval-augmented` | `rag-pipeline.svg` | `RagPipeline.tscn` | DB → retrieval → LLM flow |

---

## 11. External Services & Brands

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `github` | `gh`, `git-repo` | `github.svg` | `Github.tscn` | Octocat-friendly silhouette |
| `gitlab` | `gl` | `gitlab.svg` | `Gitlab.tscn` | GitLab fox |
| `bitbucket` | `bb` | `bitbucket.svg` | `Bitbucket.tscn` | Bitbucket bucket |
| `aws` | `amazon-aws` | `aws.svg` | `Aws.tscn` | AWS cube |
| `gcp` | `google-cloud` | `gcp.svg` | `Gcp.tscn` | GCP hex |
| `azure` | `microsoft-azure` | `azure.svg` | `Azure.tscn` | Azure A |
| `vercel` | `vercel-host` | `vercel.svg` | `Vercel.tscn` | Vercel triangle |
| `netlify` | `netlify-host` | `netlify.svg` | `Netlify.tscn` | Netlify diamond |
| `cloudflare` | `cf`, `cloudflare-edge` | `cloudflare.svg` | `Cloudflare.tscn` | Orange cloud |
| `stripe` | `payments-stripe` | `stripe.svg` | `Stripe.tscn` | Stripe S |
| `paypal` | `payments-paypal` | `paypal.svg` | `Paypal.tscn` | PayPal P |
| `twilio` | `sms-twilio`, `voice-twilio` | `twilio.svg` | `Twilio.tscn` | Twilio T |
| `sendgrid` | `email-sendgrid` | `sendgrid.svg` | `Sendgrid.tscn` | Sendgrid S |
| `auth0` | `auth0-idp` | `auth0.svg` | `Auth0.tscn` | Auth0 wave |
| `okta` | `okta-sso` | `okta.svg` | `Okta.tscn` | Okta swirl |
| `notion` | `notion-page` | `notion.svg` | `Notion.tscn` | Notion N |
| `linear` | `linear-issue` | `linear.svg` | `Linear.tscn` | Linear angle |
| `jira` | `atlassian-jira` | `jira.svg` | `Jira.tscn` | Jira J |
| `figma` | `figma-design` | `figma.svg` | `Figma.tscn` | Figma rings |
| `slack-app` | `slack-platform` | `slack-app.svg` | `SlackApp.tscn` | Slack 4-color hash |
| `discord-app` | `discord-platform` | `discord-app.svg` | `DiscordApp.tscn` | Discord controller |
| `openai` | `openai-api` | `openai.svg` | `Openai.tscn` | OpenAI hex flower |
| `anthropic` | `anthropic-api`, `claude-api` | `anthropic.svg` | `Anthropic.tscn` | Anthropic A |

---

## 12. Business & Workflow Entities

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `project` | `forge-project` | `project.svg` | `Project.tscn` | Briefcase |
| `task` | `todo`, `ticket-task` | `task.svg` | `Task.tscn` | Checkbox |
| `issue` | `bug-ticket`, `github-issue` | `issue.svg` | `Issue.tscn` | Issue circle |
| `pr` | `pull-request`, `merge-request`, `mr` | `pr.svg` | `Pr.tscn` | Branch merge icon |
| `commit` | `git-commit` | `commit.svg` | `Commit.tscn` | Dot on line |
| `branch` | `git-branch` | `branch.svg` | `Branch.tscn` | Y branch |
| `release` | `version-tag`, `git-tag` | `release.svg` | `Release.tscn` | Tag with version |
| `deployment` | `deploy`, `release-deploy` | `deployment.svg` | `Deployment.tscn` | Rocket up |
| `subscription` | `sub`, `billing-sub` | `subscription.svg` | `Subscription.tscn` | Calendar with $ |
| `invoice` | `bill`, `receipt` | `invoice.svg` | `Invoice.tscn` | Document with $ |
| `payment` | `transaction`, `charge` | `payment.svg` | `Payment.tscn` | Card swipe |
| `customer` | `client-account` | `customer.svg` | `Customer.tscn` | User with $ tag |

---

## 13. Time & Control Flow

| Slug | Aliases | 2D Icon | 3D Shape | Description |
|---|---|---|---|---|
| `timer` | `delay`, `wait` | `timer.svg` | `Timer.tscn` | Hourglass |
| `cron-trigger` | `cron`, `crontab-entry` | `cron-trigger.svg` | `CronTrigger.tscn` | Clock with `*` |
| `event-trigger` | `trigger`, `event` | `event-trigger.svg` | `EventTrigger.tscn` | Lightning bolt |
| `rate-limit` | `throttle`, `rate-limiter` | `rate-limit.svg` | `RateLimit.tscn` | Speedometer with limit line |
| `circuit-breaker` | `breaker`, `fault-tolerance` | `circuit-breaker.svg` | `CircuitBreaker.tscn` | Switch in panel |
| `retry-policy` | `retry`, `backoff` | `retry-policy.svg` | `RetryPolicy.tscn` | Circular arrow |
| `loop` | `iteration`, `for-each` | `loop.svg` | `Loop.tscn` | Closed circle arrow |
| `branch-decision` | `if`, `decision`, `condition` | `branch-decision.svg` | `BranchDecision.tscn` | Diamond with Y/N |

---

## 14. Forge-Specific (existing modules to preserve)

These already exist in FORGE and the catalog must not collide with their visual identity.

| Slug | FORGE class | Description |
|---|---|---|
| `forge-module-productive` | `Forge.Godot.Modules.ProductiveModule` | Generic productive cube (default fallback) |
| `forge-module-cluster` | `Forge.Godot.Modules.ClusterManager` | Cluster manager parent module |
| `forge-district` | `Forge.Godot.Modules.DistrictBoundaryNode` | Boundary district node |
| `forge-dna-helix` | (existing DNA viz) | DNA visualization helix |
| `forge-scanner-host` | (Phase K scanner) | Scanned network host |

---

## Fallback hierarchy

When the parser sees `<X>` for an unknown slug:

1. Try exact slug match → load asset
2. Try alias match → load parent asset
3. Try lowercase + kebab-case normalization (e.g., `<MyServer>` → `my-server`) → repeat 1-2
4. Try fuzzy distance match (Levenshtein ≤ 2) on category-relevant slugs only
5. Fallback to **category default** based on the verb (e.g., `enregistrer` verb defaults to `db-sql`, `envoyer` defaults to `email-out`, etc.)
6. Final fallback : generic FORGE productive cube + text label = `<X>` rendered as text

This way a typo or a brand-new term still renders something sensible, and the user sees a tooltip "Did you mean `db-sql`?" suggesting the closest known slug.

---

## Implementation phases (FORGE integration plan)

This catalog drives **3 phases** in the future FORGE integration plan:

- **Phase A** : Build `IconLibrary/` with the ~150 SVGs (procedural / hand / AI-assisted generation OK)
- **Phase B** : Build `ModuleShapes/` with the ~150 `.tscn` 3D primitives (start with category defaults, add specifics iteratively)
- **Phase C** : Build `DnaToScene.cs` resolver — slug lookup, fallback hierarchy, asset cache, theme application

Phase A can ship first (icons unlock the 2D panels), Phase B+C deliver the 3D experience.

---

## Counts (v1 catalog)

| Section | Slugs | Aliases |
|---|---|---|
| Hardware computers | 15 | ~30 |
| Hardware network | 10 | ~20 |
| Hardware servers/storage | 14 | ~30 |
| Databases | 16 | ~30 |
| Files & content | 19 | ~50 |
| Software apps | 16 | ~25 |
| APIs & comm | 16 | ~25 |
| Auth & security | 15 | ~25 |
| Monitoring | 13 | ~20 |
| AI/ML | 12 | ~25 |
| External services | 23 | ~25 |
| Business entities | 12 | ~15 |
| Time & control | 8 | ~15 |
| FORGE-specific | 5 | — |
| **Total** | **~194 slugs** | **~335 aliases** |

A user typing any of ~530 reasonable terms in a `<...>` slot gets the right icon out of the box.

---

## Extending the catalog

To add a new slug:

1. Add a row to the relevant section table (slug, aliases, asset paths, description)
2. Drop `IconLibrary/<slug>.svg` and `ModuleShapes/<PascalCase>.tscn` in FORGE's Visual Library folder
3. Run the catalog validator (future utility) to confirm no slug/alias collision
4. Commit with message `feat(visual-library): add <slug> icon + module`

The catalog is the source of truth. The asset folders mirror it.
