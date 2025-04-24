Feature: NIP-57 Lightning Zaps
    Tests for NIP-57 Lightning Zaps implementation

    Background:
        Given a relay at "wss://localhost:5001"
        And a user Alice
        And Alice is connected to the relay

    Scenario: Create and retrieve a zap request
        When Alice publishes an event with kind 9734 and tags:
            | relays | wss://relay1.example.com | wss://relay2.example.com |
            | amount | 21000 |
            | lnurl | lnurl1dp68gurn8ghj7um5v93kketj9ehx2amn9uh8wetvdskkkmn0wahz7mrww4excup0dajx2mrv92x9xp |
            | p | 04c915daefee38317fa734444acee390a8269fe5810b2241e5e6dd343dfbecc9 |
        Then the relay accepts the event
        When Alice subscribes to events with kind 9734
        Then Alice receives 1 event
        And the event has tag "p" with value "04c915daefee38317fa734444acee390a8269fe5810b2241e5e6dd343dfbecc9"

    Scenario: Create and retrieve a zap receipt
        When Alice publishes an event with kind 9735 and tags:
            | p | 32e1827635450ebb3c5a7d12c1f8e7b2b514439ac10a67eef3d9fd9c5c68e245 |
            | bolt11 | lnbc10u1p3unwfusp5t9r3yymhpfqculx78u027lxspgxcr2n2987mx2j55nnfs95nxnzqpp5jmrh92pfld78spqs78v9euf2385t83uvpwk9ldrlvf6ch7tpascqhp5zvkrmemgth3tufcvflmzjzfvjt023nazlhljz2n9hattj4f8jq8qxqyjw5qcqpjrzjqtc4fc44feggv7065fqe5m4ytjarg3repr5j9el35xhmtfexc42yczarjuqqfzqqqqqqqqlgqqqqqqgq9q9qxpqysgq079nkq507a5tw7xgttmj4u990j7wfggtrasah5gd4ywfr2pjcn29383tphp4t48gquelz9z78p4cq7ml3nrrphw5w6eckhjwmhezhnqpy6gyf0 |
            | description | {"pubkey":"32e1827635450ebb3c5a7d12c1f8e7b2b514439ac10a67eef3d9fd9c5c68e245","content":"","id":"d9cc14d50fcb8c27539aacf776882942c1a11ea4472f8cdec1dea82fab66279d","created_at":1674164539,"sig":"77127f636577e9029276be060332ea565deaf89ff215a494ccff16ae3f757065e2bc59b2e8c113dd407917a010b3abd36c8d7ad84c0e3ab7dab3a0b0caa9835d","kind":9734,"tags":[["e","3624762a1274dd9636e0c552b53086d70bc88c165bc4dc0f9e836a1eaf86c3b8"],["p","32e1827635450ebb3c5a7d12c1f8e7b2b514439ac10a67eef3d9fd9c5c68e245"],["relays","wss://relay.damus.io","wss://nostr-relay.wlvs.space","wss://nostr.fmt.wiz.biz","wss://relay.nostr.bg","wss://nostr.oxtr.dev","wss://nostr.v0l.io","wss://brb.io","wss://nostr.bitcoiner.social","ws://monad.jb55.com:8080","wss://relay.snort.social"]]} |
            | preimage | 5d006d2cf1e73c7148e7519a4c68adc81642ce0e25a432b2434c99f97344c15f |
        Then the relay accepts the event
        When Alice subscribes to events with kind 9735
        Then Alice receives 1 event
        And the event has tag "bolt11" with value starting with "lnbc10u1p3unwfusp5t9r3yymhpfqculx78u027lxspgxcr2n2987mx2j55nnfs95nxnzqpp5jmrh92pfld78spqs78v9euf2385t83uvpwk9ldrlvf6ch7tpascqhp5zvkrmemgth3tufcvflmzjzfvjt023nazlhljz2n9hattj4f8jq8qxqyjw5qcqpjrzjqtc4fc44feggv7065fqe5m4ytjarg3repr5j9el35xhmtfexc42yczarjuqqfzqqqqqqqqlgqqqqqqgq9q9qxpqysgq079nkq507a5tw7xgttmj4u990j7wfggtrasah5gd4ywfr2pjcn29383tphp4t48gquelz9z78p4cq7ml3nrrphw5w6eckhjwmhezhnqpy6gyf0"
