# Abp.Mqtt.Rpc Porotocol

## Topic

### Rpc Server Side

#### Unicast

Subscripbe the request topic: `from/+/rpc/to/{ServerId}`

The `ServerId` is the rpc-server own client-id of mqtt.

The `+` will match any client-id of rpc-client.
You can resolve the rpc caller identifier from it while the server received a message.

After processing, publish the result to response topic: `from/{ServerId}/rpc_response/to/{ClientId}`


#### Server in a cluster

Topic: `$share/{ClusterId}/from/+/rpc/to/{ClusterId}`

The `ClusterId` should be a valid identifier that used to identify a cluster.

The `+` is same as [#Unicast](#Unicast)

Response topic is also same as [#Unicast](#Unicsast)

#### Boradcast (TODO: Not implement)

Anycast Topic: `from/+/rpc`

It's a broadcast call to any exists session of client. (We recommend always working around session instead of connection. For more about session please reference [MQTTv5](https://docs.oasis-open.org/mqtt/mqtt/v5.0/mqtt-v5.0.html))

Grouped Multicast Topic: `from/+/rpc/to/{GroupId}`

Other things about topic are same as [#Unicast](#Unicast).

### Rpc Client Side

Publish call request to `from/{ClientId}/rpc` for anycast, or `from/{ClientId}/rpc/to/{target}` for specific target.

The `{ClientId` is the rpc-client 
The `{target}` should be an id of server, cluster or multicast-group.

Subscribe `from/+/rpc_response/to/{ClientId}` for getting response.

### Example

A Client called `Node01` and server called `Node02`.

Request topic: `from/Node01/rpc/to/Node02`

Response topic: `from/Node02/rpc_response/to/Node01`
