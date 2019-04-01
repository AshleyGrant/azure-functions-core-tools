```bash


func init --python

func new

func global language python

func pack --python

##############################################

func deploy --platform kubernetes 
func deploy --platform appservice




func appservice deploy
func kubernetes deploy
func aci deploy




func kubernetes deploy
func kubernetes template


func kubernetes deploy --name --image-name --skip-image-check --enable-secret-access

func kubernetes init

func pack --python -o file.zip

az functionapp publish --name ahmels-app --zip file.zip


###############################################

func keys generate/get --function HttpTrigger --name github-key
> key saved in secrets/httptrigger.json
> 837hriwkef7yhweihfiwuehfwohiefh83j9owjf098j3409ojf0934jf09

func keys generate/get --master 
> master key saved in secrets/host.json
> wiuehrf9834jn29jr09iomseoipfn324980jn3489oihjr8932f4uj

func keys generate/get --functions --name azure-key
> master key saved in secrets/host.json
> w9if8h9834jfoi34hj834o9hjfn8h3498jf903j409fj33o4ihfjkb34if

# require az loged in the subscription where functionapp is. 
func azure keys generate ... --app functionapp --quiet
> dnasiudh8923hd92h38dj23hdjh209ui3208n938wjhf983jh4ff92384yf

# kubernetes
# requires kubectl
func kubernetes keys generate ... --deployment-name {deployment-name} --namespace {namespace}

# displays the template for adding the key
func kubernetes keys generate ... --deployment-name {deployment-name} --namespace {namespace} --dry-run

func azure functionapp publish ... --include-secrets

func kubernetes deploy --include-secrets --secrets-type files


```