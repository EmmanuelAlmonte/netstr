Feature: NIP-11
	Relays may provide server metadata to clients to inform them of capabilities, administrative contacts, and various server attributes.
	This is made available as a JSON document over HTTP, on the same URI as the relay's websocket.

Background: 
	Given a relay is running
	And Alice is connected to relay
	| PublicKey                                                        | PrivateKey                                                      |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 512a14752ed58380496920da432f1c0cdad952cd4afda3d9bfa51c2051f91b02 |
	
Scenario: Relay sends an information document
	GET HTTP request to the websockets endpoint with a application/nostr+json Accept header should
	produce a json Relay Information Document
	When Alice sends a GET HTTP request to its websockets endpoint
	| Header | Value                  |
	| Accept | application/nostr+json |
	Then Alice receives a response with headers
	| Header                       | Value        |
	| Access-Control-Allow-Origin  | *            |
	| Access-Control-Allow-Headers | *            |
	| Access-Control-Allow-Methods | GET, OPTIONS |
	And Alice receives a response with json content
	| Field          | Type   |
	| name           | string |
	| description    | string |
	| contact        | string |
	| pubkey         | string |
	| software       | string |
	| version        | string |
	| supported_nips | int[]  |

Scenario: Relay accepts multi-value metadata Accept header
	When Alice sends a GET HTTP request to its websockets endpoint
	| Header | Value                                     |
	| Accept | text/html, application/nostr+json; q=0.9 |
	Then Alice receives a response with headers
	| Header                       | Value        |
	| Access-Control-Allow-Origin  | *            |
	| Access-Control-Allow-Headers | *            |
	| Access-Control-Allow-Methods | GET, OPTIONS |
	And Alice receives a response with json content
	| Field          | Type   |
	| name           | string |
	| description    | string |
	| contact        | string |
	| pubkey         | string |
	| software       | string |
	| version        | string |
	| supported_nips | int[]  |
