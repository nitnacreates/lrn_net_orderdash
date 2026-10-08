<?xml version="1.0" encoding="UTF-8"?>
<!-- Canonical stock levels -> partner stock XML (§16). -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes" />

  <xsl:template match="/stockLevels">
    <Stock>
      <xsl:for-each select="level">
        <Item>
          <Sku><xsl:value-of select="@sku" /></Sku>
          <Warehouse><xsl:value-of select="@warehouse" /></Warehouse>
          <OnHand><xsl:value-of select="@onHand" /></OnHand>
          <Available><xsl:value-of select="@available" /></Available>
          <AsOf><xsl:value-of select="@asOf" /></AsOf>
        </Item>
      </xsl:for-each>
    </Stock>
  </xsl:template>
</xsl:stylesheet>
