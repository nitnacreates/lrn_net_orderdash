<?xml version="1.0" encoding="UTF-8"?>
<!-- Partner order XML -> canonical order (§16). channelKey is passed in by the connector. -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes" omit-xml-declaration="yes" />
  <xsl:param name="channelKey" select="'unknown'" />

  <xsl:template match="/PurchaseOrder">
    <order>
      <channelKey><xsl:value-of select="$channelKey" /></channelKey>
      <orderNumber><xsl:value-of select="OrderNumber" /></orderNumber>
      <customerRef><xsl:value-of select="CustomerRef" /></customerRef>
      <orderDate><xsl:call-template name="isoDate"><xsl:with-param name="value" select="OrderDate" /></xsl:call-template></orderDate>
      <requiredDate><xsl:call-template name="isoDate"><xsl:with-param name="value" select="RequiredDate" /></xsl:call-template></requiredDate>

      <customerName><xsl:value-of select="Customer/Name" /></customerName>
      <phone><xsl:value-of select="Customer/Phone" /></phone>
      <email><xsl:value-of select="Customer/Email" /></email>
      <line1><xsl:value-of select="Customer/Address/Line1" /></line1>
      <line2><xsl:value-of select="Customer/Address/Line2" /></line2>
      <town><xsl:value-of select="Customer/Address/Town" /></town>
      <county><xsl:value-of select="Customer/Address/County" /></county>
      <country><xsl:value-of select="Customer/Address/Country" /></country>
      <postcode><xsl:value-of select="Customer/Address/Postcode" /></postcode>

      <lines>
        <xsl:for-each select="Lines/Line">
          <line>
            <sku><xsl:value-of select="Sku" /></sku>
            <lineRef><xsl:value-of select="LineRef" /></lineRef>
            <quantity><xsl:value-of select="Quantity" /></quantity>
            <unitPrice><xsl:value-of select="UnitPrice" /></unitPrice>
            <taxCode><xsl:value-of select="TaxCode" /></taxCode>
          </line>
        </xsl:for-each>
      </lines>
    </order>
  </xsl:template>

  <!-- the partner sends DD/MM/YYYY; canonical wants YYYY-MM-DD -->
  <xsl:template name="isoDate">
    <xsl:param name="value" />
    <xsl:choose>
      <xsl:when test="string-length($value) = 10 and substring($value, 3, 1) = '/'">
        <xsl:value-of select="concat(substring($value, 7, 4), '-', substring($value, 4, 2), '-', substring($value, 1, 2))" />
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$value" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>
