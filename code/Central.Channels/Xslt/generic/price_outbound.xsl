<?xml version="1.0" encoding="UTF-8"?>
<!-- Canonical price list -> partner price XML (§16). -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes" />

  <xsl:template match="/priceList">
    <PriceList>
      <EffectiveFrom><xsl:value-of select="effectiveFrom" /></EffectiveFrom>
      <EffectiveTo><xsl:value-of select="effectiveTo" /></EffectiveTo>
      <xsl:for-each select="items/item">
        <Price>
          <Sku><xsl:value-of select="@sku" /></Sku>
          <Amount currency="{@currency}"><xsl:value-of select="@unitPrice" /></Amount>
        </Price>
      </xsl:for-each>
    </PriceList>
  </xsl:template>
</xsl:stylesheet>
