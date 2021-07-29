package com.github.lilbaek.loaditrider.model

import java.util.HashMap
import javax.xml.namespace.NamespaceContext

class SimpleNamespaceContext : NamespaceContext {
    private val prefixMap: MutableMap<String, String> = HashMap()

    constructor() {}
    constructor(prefix: String, uri: String) {
        prefixMap[prefix] = uri
    }

    fun addPrefixMapping(prefix: String, uri: String) {
        prefixMap[prefix] = uri
    }

    override fun getNamespaceURI(prefix: String): String? {
        return if (prefixMap.containsKey(prefix)) {
            prefixMap[prefix]!!
        } else null
    }

    override fun getPrefix(namespaceURI: String): String? {
        return null
    }

    override fun getPrefixes(namespaceURI: String): MutableIterator<String>? {
        return null
    }
}